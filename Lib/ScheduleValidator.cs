using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CloudLibrary.Lib
{
    public static class ScheduleValidator
    {
        private static int DaysToValidate = 7;

        private static DateTime SetTimeZone(DateTime timeToConvert, string timeZone)
        {
            TimeZoneInfo est = TimeZoneInfo.FindSystemTimeZoneById(timeZone);
            DateTime targetTime = TimeZoneInfo.ConvertTime(timeToConvert, est);
            return targetTime;
        }

        private static DateTime UnixToDateTime(long timeInSeconds, string timeZone)
        {
            DateTimeOffset dateTimeOffset = DateTimeOffset.FromUnixTimeSeconds(timeInSeconds);
            DateTime dateTime = dateTimeOffset.DateTime;
            return SetTimeZone(dateTime, timeZone);
        }

        public static bool ValidateSchedule(JToken weekSchedule, long blockTime, string timeZone)
        {
            Dictionary<int, List<DateTime>> scheduleSlots = new Dictionary<int, List<DateTime>>();
            int scheduleSlotCounter = 0;

            DateTime today = SetTimeZone(DateTime.Today, timeZone);
            List<dynamic> dateObjects = new List<dynamic>() { today };

            for (int i = 1; i < DaysToValidate; i++)
            {
                DateTime day = today.AddDays(i);
                dateObjects.Add(day);
            }

            foreach (var date in dateObjects)
            {
                foreach (var daySchedule in weekSchedule)
                {
                    if ((int)daySchedule["dayOfWeek"] == (int)date.DayOfWeek)
                    {
                        foreach (var day in daySchedule["times"])
                        {
                            var startTime = day["start"].ToString().Split(":");
                            var endTime = day["end"].ToString().Split(":");

                            DateTime startDateTime = new DateTime(date.Year, date.Month, date.Day,
                                int.Parse(startTime[0]), int.Parse(startTime[1]), 0);

                            DateTime endDateTime = new DateTime(date.Year, date.Month, date.Day, int.Parse(endTime[0]),
                                int.Parse(endTime[1]), 0);

                            scheduleSlots[scheduleSlotCounter] = new List<DateTime>() { startDateTime, endDateTime };
                            scheduleSlotCounter++;
                        }
                    }
                }
            }

            DateTime blockDateTime = UnixToDateTime(blockTime, timeZone);
            bool result = false;

            Parallel.For(0, scheduleSlotCounter, (n, state) =>
            {
                DateTime start = scheduleSlots[n][0];
                DateTime stop = scheduleSlots[n][1];

                if (start <= blockDateTime && blockDateTime <= stop)
                {
                    result = true;
                    state.Stop();
                }
            });

            return result;
        }
    }
}