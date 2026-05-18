// ============================================================
// Shared.CL / Helpers / AuditHttpClient.cs
// ============================================================

using System.Net.Http.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace LifeTrack.Shared.Helpers
{
    public class AuditHttpClient
    {
        private readonly HttpClient _http;
        private readonly IHttpContextAccessor _contextAccessor;
        private readonly ILogger<AuditHttpClient> _logger;

        private const string AuditEndpoint = "http://localhost:5008/api/audit/log";

        public AuditHttpClient(
            HttpClient http,
            IHttpContextAccessor contextAccessor,
            ILogger<AuditHttpClient> logger)
        {
            _http = http;
            _contextAccessor = contextAccessor;
            _logger = logger;
        }

        /// <summary>
        /// Fire-and-forget audit log.
        /// Pass explicitUserId for operations where JWT is not yet available (e.g. login).
        /// </summary>
        public void Log(
            string action,
            string entityType,
            long? entityId = null,
            string details = "",
            long? explicitUserId = null)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    long resolvedUserId = 0;

                    if (explicitUserId.HasValue && explicitUserId.Value > 0)
                    {
                        // Use the explicitly passed userId (for login/register)
                        resolvedUserId = explicitUserId.Value;
                    }
                    else
                    {
                        // Try to get from JWT token in context
                        var context = _contextAccessor.HttpContext;
                        var userIdStr = context?.User?
                            .FindFirst(ClaimTypes.NameIdentifier)?.Value;
                        long.TryParse(userIdStr, out resolvedUserId);
                    }

                    var payload = new
                    {
                        UserID = resolvedUserId,
                        Action = action,
                        EntityType = entityType,
                        EntityID = entityId,
                        Details = details
                    };

                    await _http.PostAsJsonAsync(AuditEndpoint, payload);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(
                        "Audit log failed for Action={Action} EntityType={EntityType}: {Message}",
                        action, entityType, ex.Message);
                }
            });
        }
    }
}