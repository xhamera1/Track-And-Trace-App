namespace _10.Services
{
    public class ServiceResult<T>
    {
        public bool IsSuccess { get; private set; }
        public T? Data { get; private set; }
        public string? ErrorMessage { get; private set; }
        public string? ErrorCode { get; private set; }

        private ServiceResult(bool isSuccess, T? data, string? errorMessage, string? errorCode = null)
        {
            IsSuccess = isSuccess;
            Data = data;
            ErrorMessage = errorMessage;
            ErrorCode = errorCode;
        }

        public static ServiceResult<T> Success(T data)
        {
            return new ServiceResult<T>(true, data, null);
        }

        public static ServiceResult<T> Failure(string errorMessage, string? errorCode = null)
        {
            return new ServiceResult<T>(false, default, errorMessage, errorCode);
        }
        public static ServiceResult<T> NotFound(string resourceName, object id)
        {
            return new ServiceResult<T>(false, default, $"{resourceName} with ID {id} not found.", "NOT_FOUND");
        }

        public static ServiceResult<T> ValidationFailure(string errorMessage)
        {
            return new ServiceResult<T>(false, default, errorMessage, "VALIDATION_ERROR");
        }

        public static ServiceResult<T> Conflict(string errorMessage)
        {
            return new ServiceResult<T>(false, default, errorMessage, "CONFLICT");
        }
    }
}
