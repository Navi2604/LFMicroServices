using System.Security.Claims;
using System.Text;
using System.Text.Json;
using LifeTrack.Shared.DTOs;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace LifeTrack.Shared.Middleware
{
    public class AuditMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<AuditMiddleware> _logger;
        private readonly string _serviceName;
        private readonly string _auditServiceUrl;

        public AuditMiddleware(
            RequestDelegate next,
            ILogger<AuditMiddleware> logger,
            IConfiguration config,
            string serviceName)
        {
            _next = next;
            _logger = logger;
            _serviceName = serviceName;
            _auditServiceUrl = config["ServiceUrls:AuditService"]
                               ?? "http://localhost:5008";
        }

        public async Task InvokeAsync(HttpContext context)
        {
            await _next(context);

            // Fire and forget — don't await
            _ = SendAuditLogAsync(context);
        }

        private async Task SendAuditLogAsync(HttpContext context)
        {
            try
            {
                if (!context.Request.Path
                    .StartsWithSegments("/api"))
                    return;

                var userId = context.User?
                .Claims
                .FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?
                .Value ?? "anonymous";

                var auditLog = new AuditLogRequest
                {
                    UserId = userId,
                    Action = $"{context.Request.Method} " +
                                $"{context.Request.Path}",
                    Timestamp = DateTime.UtcNow
                };

                using var httpClient = new HttpClient();
                var json = JsonSerializer.Serialize(auditLog);
                var content = new StringContent(
                    json, Encoding.UTF8, "application/json");

                await httpClient.PostAsync(
                    $"{_auditServiceUrl}/api/audit/log", content);
            }
            catch
            {
                _logger.LogWarning(
                    "Could not send audit log to AuditService.");
            }
        }
    }
}