namespace PoultrySlaughterPOS.Models.DTOs
{
    /// <summary>
    /// Generic service result wrapper providing standardized response structure
    /// Implements Result pattern for robust error handling and status communication
    /// </summary>
    /// <typeparam name="T">Result data type</typeparam>
    public class ServiceResult<T>
    {
        public bool IsSuccess { get; set; }
        public T? Data { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<string> Errors { get; set; } = new();
        public string? ErrorCode { get; set; }
        public Exception? Exception { get; set; }

        public static ServiceResult<T> Success(T data, string message = "Operation completed successfully")
        {
            return new ServiceResult<T>
            {
                IsSuccess = true,
                Data = data,
                Message = message
            };
        }

        public static ServiceResult<T> Failure(string message, string? errorCode = null, Exception? exception = null)
        {
            return new ServiceResult<T>
            {
                IsSuccess = false,
                Message = message,
                ErrorCode = errorCode,
                Exception = exception,
                Errors = new List<string> { message }
            };
        }

        public static ServiceResult<T> Failure(List<string> errors, string message = "Operation failed")
        {
            return new ServiceResult<T>
            {
                IsSuccess = false,
                Message = message,
                Errors = errors
            };
        }
    }
}