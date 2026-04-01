namespace FinTrackPortal.Common
{
    /// <summary>
    /// Standardised JSON envelope returned by every API endpoint.
    /// Controllers use SuccessResponse and ErrorResponse
    /// factory methods to ensure a consistent shape for all HTTP responses.
    /// </summary>
    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public T? Data { get; set; }
        public List<string>? Errors { get; set; }

        public static ApiResponse<T> SuccessResponse(T data, string message = "Request completed successfully")
            => new() { Success = true, Message = message, Data = data, Errors = null };

        public static ApiResponse<object?> ErrorResponse(string message, List<string>? errors = null)
            => new ApiResponse<object?> { Success = false, Message = message, Data = null, Errors = errors };

        public static ApiResponse<object?> ErrorResponse(string message, string error)
            => new ApiResponse<object?> { Success = false, Message = message, Data = null, Errors = new List<string> { error } };
    }
}
