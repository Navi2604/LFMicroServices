// ============================================================
// Shared.CL / Wrappers / ApiResponse.cs
// ============================================================

namespace LifeTrack.Shared.Wrappers
{
    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public T? Data { get; set; }
        public string[] Errors { get; set; } = Array.Empty<string>();

        public static ApiResponse<T> Ok(T data, string message = "Success")
            => new() { Success = true, Message = message, Data = data };

        public static ApiResponse<T> Fail(string message, string[]? errors = null)
            => new()
            {
                Success = false,
                Message = message,
                Errors = errors ?? Array.Empty<string>()
            };

        public static ApiResponse<T> ValidationFail(string[] errors)
            => new()
            {
                Success = false,
                Message = "Validation failed.",
                Errors = errors
            };
    }
}