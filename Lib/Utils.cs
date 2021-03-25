using System;

namespace CloudLibrary.Lib
{
    public static class Utils
    {
        public static long GetUnixTimestamp()
        {
            TimeSpan time = (DateTime.UtcNow - DateTime.UnixEpoch);
            long timestamp = (long)time.TotalSeconds;
            return timestamp;
        }

        public static long GetFutureTimeStamp(long minutes)
        {
            return GetUnixTimestamp() + (minutes * 60);
        }

    }
}
