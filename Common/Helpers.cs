namespace Chefster.Common;

public static class Helpers
{
    public static DateTime GetUserCurrentTime(string timeZone)
    {
        var timeZoneInfo = TimeZoneInfo.FindSystemTimeZoneById(timeZone);
        return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, timeZoneInfo);
    }
}