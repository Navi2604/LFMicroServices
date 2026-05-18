// ============================================================
// Shared.CL / Middleware / ExceptionMiddleware.cs
// Global middleware fallback — catches anything the filter misses
// ============================================================

using System.Net;
using System.Text.Json;
using LifeTrack.Shared.Wrappers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace LifeTrack.Shared.Middleware
{
    public class ExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionMiddleware> _logger;

        public ExceptionMiddleware(
            RequestDelegate next,
            ILogger<ExceptionMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Middleware caught unhandled exception on {Method} {Path}",
                    context.Request.Method,
                    context.Request.Path);

                await HandleExceptionAsync(context, ex);
            }
        }

        private static async Task HandleExceptionAsync(
            HttpContext context, Exception exception)
        {
            var (statusCode, message) = exception switch
            {
                UnauthorizedAccessException =>
                    (HttpStatusCode.Unauthorized,
                     "You are not authorized."),

                KeyNotFoundException =>
                    (HttpStatusCode.NotFound,
                     "The requested resource was not found."),

                ArgumentException =>
                    (HttpStatusCode.BadRequest,
                     exception.Message),

                InvalidOperationException =>
                    (HttpStatusCode.BadRequest,
                     exception.Message),

                _ =>
                    (HttpStatusCode.InternalServerError,
                     "An unexpected error occurred.")
            };

            var response = ApiResponse<object>.Fail(message);
            var json = JsonSerializer.Serialize(response,
                new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)statusCode;

            await context.Response.WriteAsync(json);
        }
    }

    // Extension method for clean registration in Program.cs
    public static class ExceptionMiddlewareExtensions
    {
        public static IApplicationBuilder UseGlobalExceptionHandler(
            this IApplicationBuilder app)
            => app.UseMiddleware<ExceptionMiddleware>();
    }
}