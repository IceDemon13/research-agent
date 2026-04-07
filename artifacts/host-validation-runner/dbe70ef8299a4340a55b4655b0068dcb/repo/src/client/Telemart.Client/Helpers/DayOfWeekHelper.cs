using System;
using System.Collections.Generic;

namespace Telemart.Client.Helpers
{
    public static class DayOfWeekHelper
    {
        public static IEnumerable<DayOfWeek> Parse(string daysOfWeekString)
        {
            if (string.IsNullOrWhiteSpace(daysOfWeekString))
            {
                yield break;
            }

            foreach (char dayOfWeekChar in daysOfWeekString)
            {
                switch (dayOfWeekChar)
                {
                    case '1':
                        yield return DayOfWeek.Monday;

                        break;
                    case '2':
                        yield return DayOfWeek.Tuesday;

                        break;
                    case '3':
                        yield return DayOfWeek.Wednesday;

                        break;
                    case '4':
                        yield return DayOfWeek.Thursday;

                        break;
                    case '5':
                        yield return DayOfWeek.Friday;

                        break;
                    case '6':
                        yield return DayOfWeek.Saturday;

                        break;
                    case '7':
                        yield return DayOfWeek.Sunday;

                        break;
                }
            }
        }
    }
}