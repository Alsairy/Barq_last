namespace BARQ.Core.Models.Responses
{
    public sealed class ApiResponse<T>
    {
        public bool Success { get; init; }
        public string? Message { get; init; }
        public T? Data { get; init; }
        public string[]? Errors { get; init; }

        public static ApiResponse<T> Ok(T? data, string? message = null) =>
            new() { Success = true, Data = data, Message = message };

        public static ApiResponse<T> Fail(string message, params string[] errors) =>
            new() { Success = false, Message = message, Errors = errors?.Length > 0 ? errors : null };
    }
}
