// ============================================================
// Shared.CL / Filters / GlobalExceptionFilter.cs
// ============================================================

using LifeTrack.Shared.Wrappers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;

namespace LifeTrack.Shared.Filters
{
    public class GlobalExceptionFilter : IExceptionFilter
    {
        private readonly ILogger<GlobalExceptionFilter> _logger;

        public GlobalExceptionFilter(ILogger<GlobalExceptionFilter> logger)
        {
            _logger = logger;
        }

        public void OnException(ExceptionContext context)
        {
            var exception = context.Exception;

            // Log full details server-side only
            _logger.LogError(
                exception,
                "Unhandled exception on {Method} {Path}",
                context.HttpContext.Request.Method,
                context.HttpContext.Request.Path);

            // Map exception type to status code and message
            var (statusCode, message) = exception switch
            {
                UnauthorizedAccessException =>
                    (401, "You are not authorized to perform this action."),

                KeyNotFoundException =>
                    (404, "The requested resource was not found."),

                ArgumentNullException =>
                    (400, "A required value was missing."),

                ArgumentException =>
                    (400, exception.Message),

                InvalidOperationException =>
                    (400, exception.Message),

                _ =>
                    (500, "An unexpected error occurred. Please try again later.")
            };

            var response = ApiResponse<object>.Fail(message);

            context.Result = new ObjectResult(response)
            {
                StatusCode = statusCode
            };

            context.ExceptionHandled = true;
        }
    }
}