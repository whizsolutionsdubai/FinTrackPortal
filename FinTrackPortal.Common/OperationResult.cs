namespace FinTrackPortal.Common
{
    /// <summary>
    /// Generic wrapper for service/repository results.
    /// Every data-access and business-logic method returns this so callers
    /// can branch on <see cref="IsSuccess"/> without catching exceptions.
    /// </summary>
    public class OperationResult<T>
    {
        public bool IsSuccess { get; set; }
        public string? ErrorMessage { get; set; }
        public T? Data { get; set; }

        public static OperationResult<T> Success(T data) => new OperationResult<T> { IsSuccess = true, Data = data };
        public static OperationResult<T> Failure(string error) => new OperationResult<T> { IsSuccess = false, ErrorMessage = error };
    }
}
