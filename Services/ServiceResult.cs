namespace _10.Services
{
    /// <summary>
    /// Generic service result wrapper for consistent error handling
    /// </summary>
    /// <typeparam name="T">The type of data returned on success</typeparam>
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

        /// <summary>
        /// Creates a successful result
        /// </summary>
        /// <param name="data">The success data</param>
        /// <returns>ServiceResult with success status</returns>
        public static ServiceResult<T> Success(T data)
        {
            return new ServiceResult<T>(true, data, null);
        }

        /// <summary>
        /// Creates a failure result
        /// </summary>
        /// <param name="errorMessage">The error message</param>
        /// <param name="errorCode">Optional error code</param>
        /// <returns>ServiceResult with failure status</returns>
        public static ServiceResult<T> Failure(string errorMessage, string? errorCode = null)
        {
            return new ServiceResult<T>(false, default, errorMessage, errorCode);
        }

        /// <summary>
        /// Creates a not found failure result
        /// </summary>
        /// <param name="resourceName">Name of the resource that was not found</param>
        /// <param name="id">ID of the resource</param>
        /// <returns>ServiceResult with not found status</returns>
        public static ServiceResult<T> NotFound(string resourceName, object id)
        {
            return new ServiceResult<T>(false, default, $"{resourceName} with ID {id} not found.", "NOT_FOUND");
        }

        /// <summary>
        /// Creates a validation failure result
        /// </summary>
        /// <param name="errorMessage">The validation error message</param>
        /// <returns>ServiceResult with validation failure status</returns>
        public static ServiceResult<T> ValidationFailure(string errorMessage)
        {
            return new ServiceResult<T>(false, default, errorMessage, "VALIDATION_ERROR");
        }

        /// <summary>
        /// Creates a conflict failure result
        /// </summary>
        /// <param name="errorMessage">The conflict error message</param>
        /// <returns>ServiceResult with conflict failure status</returns>
        public static ServiceResult<T> Conflict(string errorMessage)
        {
            return new ServiceResult<T>(false, default, errorMessage, "CONFLICT");
        }
    }
}
