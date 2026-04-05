using System;
using System.Diagnostics;

namespace shrServices
{
    public sealed class TimeService
    {
        private static readonly DateTime _timeReference = new(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        public static int NewsTime()
        {
            return (int)Math.Truncate((TimeZoneInfo.ConvertTimeToUtc(DateTime.Now) - _timeReference).TotalSeconds);
        }

        public static int NewsTime(DateTime timeStamp)
        {
            return (int)Math.Truncate((TimeZoneInfo.ConvertTimeToUtc(timeStamp) - _timeReference).TotalSeconds);
        }

#if DEBUG
        public static DateTime GetDateTimeFrom(double totalSeconds)
        {
            return _timeReference.AddSeconds(totalSeconds).ToLocalTime();
        }
#endif

        public static int TimeDetail()
        {
            return (int)Stopwatch.GetTimestamp(); // lower part of QueryPerformanceCounter()
        }
    }
}
