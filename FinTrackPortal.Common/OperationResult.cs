namespace FinTrackPortal.Common
{
    public class OperationResult<T>
    {
        public bool IsSuccess { get; set; }
        public string? ErrorMessage { get; set; }
        public T? Data { get; set; }

        public static OperationResult<T> Success(T data) => new OperationResult<T> { IsSuccess = true, Data = data };
        public static OperationResult<T> Failure(string error) => new OperationResult<T> { IsSuccess = false, ErrorMessage = error };
    }
}
