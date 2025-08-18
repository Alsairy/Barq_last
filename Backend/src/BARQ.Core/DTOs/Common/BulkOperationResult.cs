namespace BARQ.Core.DTOs.Common
{
    public class BulkOperationResult
    {
        public int SuccessCount { get; set; }
        public int FailureCount { get; set; }
        public List<string> Errors { get; set; } = new();
        public List<Guid> SuccessfulIds { get; set; } = new();
        public List<Guid> FailedIds { get; set; } = new();
        
        public bool IsSuccess => FailureCount == 0;
        public int TotalCount => SuccessCount + FailureCount;
    }

    public class BulkDeleteRequest
    {
        public List<Guid> Ids { get; set; } = new();
        public bool PermanentDelete { get; set; } = false;
    }

    public class BulkRestoreRequest
    {
        public List<Guid> Ids { get; set; } = new();
    }

    public class BulkUpdateRequest<T>
    {
        public List<Guid> Ids { get; set; } = new();
        public T UpdateData { get; set; } = default!;
    }
}
