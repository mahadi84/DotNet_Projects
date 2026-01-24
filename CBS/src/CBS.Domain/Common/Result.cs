

namespace CBS.Domain.Common;

public class Result<T>
    {
        public bool IsSuccess { get; }
        public T Data { get; }
        public string Message { get; }
        public List<string> Errors { get; }

        protected Result(bool isSuccess, T data, string message, List<string> errors = null)
        {
            IsSuccess = isSuccess;
            Data = data;
            Message = message;
            Errors = errors ?? new List<string>();
        }

        public static Result<T> Success(T data, string message = "Operation successful")
            => new Result<T>(true, data, message);

        public static Result<T> Failure(string message, List<string> errors = null)
            => new Result<T>(false, default, message, errors);
}
