namespace BARQ.Core.DTOs.Common
{
    public class ListRequest
    {
        public int Page { get; set; } = 1;
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string? SearchTerm { get; set; }
        public string? SortBy { get; set; }
        public string? SortDirection { get; set; } = "asc";
        public bool SortDescending { get; set; } = false;
        public Dictionary<string, object>? Filters { get; set; }
    }
}
