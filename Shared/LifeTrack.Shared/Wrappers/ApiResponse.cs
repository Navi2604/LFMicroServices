namespace LifeTrack.Shared.Wrappers
{
    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public T? Data { get; set; }
        public List<string> Errors { get; set; } = new();

        public static ApiResponse<T> Ok(T data, string message = "Success")
            => new()
            {
                Success = true,
                Message = message,
                Data = data,
                Errors = new()
            };

        public static ApiResponse<T> Fail(string message,
            List<string>? errors = null)
            => new()
            {
                Success = false,
                Message = message,
                Data = default,
                Errors = errors ?? new()
            };

        public static ApiResponse<T> Fail(string message, string error)
            => new()
            {
                Success = false,
                Message = message,
                Data = default,
                Errors = new List<string> { error }
            };
    }

    public class ApiResponse : ApiResponse<object>
    {
        public static new ApiResponse<object> Ok(string message = "Success")
            => new()
            {
                Success = true,
                Message = message,
                Data = null,
                Errors = new()
            };

        public static new ApiResponse<object> Fail(string message,
            List<string>? errors = null)
            => new()
            {
                Success = false,
                Message = message,
                Data = null,
                Errors = errors ?? new()
            };
    }
}