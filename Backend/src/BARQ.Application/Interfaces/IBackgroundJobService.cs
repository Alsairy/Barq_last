namespace BARQ.Application.Interfaces
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

    public class JobInfo
    {
        public string Id { get; set; } = "";
        public string Type { get; set; } = "";
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
