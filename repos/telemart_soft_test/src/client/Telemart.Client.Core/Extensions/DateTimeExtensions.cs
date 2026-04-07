using System;

namespace Telemart.Client.Core.Extensions
{
    public static class DateTimeExtensions
    {
        public static bool IsWeekdayDayOfWeek(this DateTime dateTime)
        {
            return dateTime.DayOfWeek == DayOfWeek.Monday ||
                   dateTime.DayOfWeek == DayOfWeek.Tuesday ||
                   dateTime.DayOfWeek == DayOfWeek.Wednesday ||
                   dateTime.DayOfWeek == DayOfWeek.Thursday ||
                   dateTime.DayOfWeek == DayOfWeek.Friday;
        }

        public static bool IsWeekendDayOfWeek(this DateTime dateTime)
        {
            return dateTime.DayOfWeek == DayOfWeek.Saturday ||
                   dateTime.DayOfWeek == DayOfWeek.Sunday;
        }

        public static long GetMinutesDateTimeRelativeNow(this DateTime dateTime)
        {
            TimeSpan ts = DateTime.Now - dateTime;
            return (long)ts.TotalMinutes;
        }
    }
}