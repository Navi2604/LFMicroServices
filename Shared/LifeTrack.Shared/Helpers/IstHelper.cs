// ============================================================
// Shared / LifeTrack.Shared / Helpers / IstHelper.cs
// IST = UTC + 5:30
// Usage: IstHelper.Now  instead of  DateTime.UtcNow
// ============================================================

namespace LifeTrack.Shared.Helpers
{
    public static class IstHelper
    {
        private static readonly TimeZoneInfo _ist =
            TimeZoneInfo.FindSystemTimeZoneById(
                OperatingSystem.IsWindows()
                    ? "India Standard Time"     // Windows TZ ID
                    : "Asia/Kolkata");           // Linux/Mac TZ ID

        /// <summary>Current date-time in IST (UTC+5:30).</summary>
        public static DateTime Now =>
            TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, _ist);

        /// <summary>Convert any UTC DateTime to IST.</summary>
        public static DateTime ToIst(DateTime utc) =>
            TimeZoneInfo.ConvertTimeFromUtc(
                DateTime.SpecifyKind(utc, DateTimeKind.Utc), _ist);
    }
}