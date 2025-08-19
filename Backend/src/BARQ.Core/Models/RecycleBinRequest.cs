namespace BARQ.Core.Models
{
    public class RecycleBinRequest
    {
        public string Entity { get; set; } = string.Empty;
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }

    public class RestoreRequest
    {
        public string Entity { get; set; } = string.Empty;
        public Guid Id { get; set; }
    }
}
