using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace BARQ.Application.Services.Workflow
{
    public interface IBackgroundJobService
    {
        Task<string> EnqueueAsync<T>(Func<T, Task> job, TimeSpan? delay = null) where T : class;
        Task<string> ScheduleAsync<T>(Func<T, Task> job, DateTime scheduledTime) where T : class;
        Task<string> EnqueueRecurringAsync<T>(string jobId, Func<T, Task> job, string cronExpression) where T : class;
        Task<bool> CancelJobAsync(string jobId);
        Task<JobStatus> GetJobStatusAsync(string jobId);
        Task<List<JobInfo>> GetJobsAsync(JobStatusFilter filter = JobStatusFilter.All);
    }

    public class BackgroundJobService : BackgroundService, IBackgroundJobService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<BackgroundJobService> _logger;
        private readonly ConcurrentDictionary<string, JobInfo> _jobs = new();
        private readonly ConcurrentQueue<QueuedJob> _jobQueue = new();
        private readonly Timer _timer;
        private readonly CancellationTokenSource _cancellationTokenSource = new();

        public BackgroundJobService(IServiceProvider serviceProvider, ILogger<BackgroundJobService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _timer = new Timer(ProcessScheduledJobs, null, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(10));
        }

        public Task<string> EnqueueAsync<T>(Func<T, Task> job, TimeSpan? delay = null) where T : class
        {
            var jobId = Guid.NewGuid().ToString();
            var executeAt = DateTime.UtcNow.Add(delay ?? TimeSpan.Zero);
            
            var jobInfo = new JobInfo
            {
                Id = jobId,
                Type = typeof(T).Name,
                Status = JobStatus.Enqueued,
                CreatedAt = DateTime.UtcNow,
                ScheduledAt = executeAt
            };

            var queuedJob = new QueuedJob
            {
                Id = jobId,
                Job = async (serviceProvider) =>
                {
                    using var scope = serviceProvider.CreateScope();
                    var service = scope.ServiceProvider.GetRequiredService<T>();
                    await job(service);
                },
                ScheduledAt = executeAt
            };

            _jobs[jobId] = jobInfo;
            _jobQueue.Enqueue(queuedJob);

            _logger.LogInformation("Job {JobId} of type {JobType} enqueued for execution at {ScheduledAt}", 
                jobId, typeof(T).Name, executeAt);

            return Task.FromResult(jobId);
        }

        public Task<string> ScheduleAsync<T>(Func<T, Task> job, DateTime scheduledTime) where T : class
        {
            var delay = scheduledTime - DateTime.UtcNow;
            if (delay < TimeSpan.Zero)
                delay = TimeSpan.Zero;

            return EnqueueAsync(job, delay);
        }

        public Task<string> EnqueueRecurringAsync<T>(string jobId, Func<T, Task> job, string cronExpression) where T : class
        {
            var jobInfo = new JobInfo
            {
                Id = jobId,
                Type = typeof(T).Name,
                Status = JobStatus.Recurring,
                CreatedAt = DateTime.UtcNow,
                CronExpression = cronExpression
            };

            _jobs[jobId] = jobInfo;

            _logger.LogInformation("Recurring job {JobId} of type {JobType} scheduled with cron {CronExpression}", 
                jobId, typeof(T).Name, cronExpression);

            return Task.FromResult(jobId);
        }

        public Task<bool> CancelJobAsync(string jobId)
        {
            if (_jobs.TryGetValue(jobId, out var jobInfo))
            {
                if (jobInfo.Status == JobStatus.Enqueued || jobInfo.Status == JobStatus.Recurring)
                {
                    jobInfo.Status = JobStatus.Cancelled;
                    jobInfo.CompletedAt = DateTime.UtcNow;
                    
                    _logger.LogInformation("Job {JobId} cancelled", jobId);
                    return Task.FromResult(true);
                }
            }

            return Task.FromResult(false);
        }

        public Task<JobStatus> GetJobStatusAsync(string jobId)
        {
            if (_jobs.TryGetValue(jobId, out var jobInfo))
            {
                return Task.FromResult(jobInfo.Status);
            }

            return Task.FromResult(JobStatus.NotFound);
        }

        public Task<List<JobInfo>> GetJobsAsync(JobStatusFilter filter = JobStatusFilter.All)
        {
            var jobs = _jobs.Values.AsEnumerable();

            if (filter != JobStatusFilter.All)
            {
                jobs = filter switch
                {
                    JobStatusFilter.Active => jobs.Where(j => j.Status == JobStatus.Enqueued || j.Status == JobStatus.Running),
                    JobStatusFilter.Completed => jobs.Where(j => j.Status == JobStatus.Completed),
                    JobStatusFilter.Failed => jobs.Where(j => j.Status == JobStatus.Failed),
                    JobStatusFilter.Cancelled => jobs.Where(j => j.Status == JobStatus.Cancelled),
                    _ => jobs
                };
            }

            return Task.FromResult(jobs.OrderByDescending(j => j.CreatedAt).ToList());
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Background job service started");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessJobQueue(stoppingToken);
                    await Task.Delay(1000, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing job queue");
                    await Task.Delay(5000, stoppingToken);
                }
            }

            _logger.LogInformation("Background job service stopped");
        }

        private async System.Threading.Tasks.Task ProcessJobQueue(CancellationToken cancellationToken)
        {
            var jobsToProcess = new List<QueuedJob>();
            var now = DateTime.UtcNow;

            while (_jobQueue.TryDequeue(out var queuedJob))
            {
                if (queuedJob.ScheduledAt <= now)
                {
                    jobsToProcess.Add(queuedJob);
                }
                else
                {
                    _jobQueue.Enqueue(queuedJob);
                    break;
                }
            }

            foreach (var queuedJob in jobsToProcess)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                await ProcessJob(queuedJob, cancellationToken);
            }
        }

        private async System.Threading.Tasks.Task ProcessJob(QueuedJob queuedJob, CancellationToken cancellationToken)
        {
            if (!_jobs.TryGetValue(queuedJob.Id, out var jobInfo))
                return;

            if (jobInfo.Status == JobStatus.Cancelled)
                return;

            try
            {
                jobInfo.Status = JobStatus.Running;
                jobInfo.StartedAt = DateTime.UtcNow;

                _logger.LogInformation("Executing job {JobId} of type {JobType}", queuedJob.Id, jobInfo.Type);

                await queuedJob.Job(_serviceProvider);

                jobInfo.Status = JobStatus.Completed;
                jobInfo.CompletedAt = DateTime.UtcNow;

                _logger.LogInformation("Job {JobId} completed successfully", queuedJob.Id);
            }
            catch (Exception ex)
            {
                jobInfo.Status = JobStatus.Failed;
                jobInfo.CompletedAt = DateTime.UtcNow;
                jobInfo.ErrorMessage = ex.Message;

                _logger.LogError(ex, "Job {JobId} failed with error: {Error}", queuedJob.Id, ex.Message);
            }
        }

        private void ProcessScheduledJobs(object? state)
        {
            var recurringJobs = _jobs.Values.Where(j => j.Status == JobStatus.Recurring).ToList();
            
            foreach (var job in recurringJobs)
            {
                if (job.LastExecuted == null || DateTime.UtcNow - job.LastExecuted > TimeSpan.FromMinutes(10))
                {
                    job.LastExecuted = DateTime.UtcNow;
                    _logger.LogInformation("Triggering recurring job {JobId}", job.Id);
                }
            }
        }

        public override void Dispose()
        {
            _timer?.Dispose();
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
            base.Dispose();
        }
    }

    public class QueuedJob
    {
        public string Id { get; set; } = string.Empty;
        public Func<IServiceProvider, Task> Job { get; set; } = default!;
        public DateTime ScheduledAt { get; set; }
    }

    public class JobInfo
    {
        public string Id { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public JobStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ScheduledAt { get; set; }
        public DateTime? StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public DateTime? LastExecuted { get; set; }
        public string? ErrorMessage { get; set; }
        public string? CronExpression { get; set; }
    }

    public enum JobStatus
    {
        NotFound,
        Enqueued,
        Running,
        Completed,
        Failed,
        Cancelled,
        Recurring
    }

    public enum JobStatusFilter
    {
        All,
        Active,
        Completed,
        Failed,
        Cancelled
    }
}
