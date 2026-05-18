// ============================================================
// Shared.CL / Middleware / AuditMiddleware.cs
// Logs HTTP method + path for mutating requests automatically
// ============================================================

using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace LifeTrack.Shared.Middleware
{
    public class AuditMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<AuditMiddleware> _logger;

        // Methods that change data — these get logged
        private static readonly HashSet<string> _auditMethods =
            new(StringComparer.OrdinalIgnoreCase)
            { "POST", "PUT", "PATCH", "DELETE" };

        public AuditMiddleware(
            RequestDelegate next,
            ILogger<AuditMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            await _next(context);

            // Only log mutating requests that succeeded
            if (_auditMethods.Contains(context.Request.Method) &&
                context.Response.StatusCode is >= 200 and < 300)
            {
                var userId = context.User
                    .FindFirst(ClaimTypes.NameIdentifier)?.Value
                    ?? "anonymous";

                var method = context.Request.Method;
                var path = context.Request.Path;
                var status = context.Response.StatusCode;

                _logger.LogInformation(
                    "[AUDIT] User={UserId} | {Method} {Path} | Status={Status}",
                    userId, method, path, status);
            }
        }
    }

    public static class AuditMiddlewareExtensions
    {
        public static IApplicationBuilder UseAuditLogging(
            this IApplicationBuilder app)
            => app.UseMiddleware<AuditMiddleware>();
    }
}