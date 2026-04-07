using System;
using System.Collections.Generic;
using System.Linq;
using Telemart.Client.Core.Helpers;
using Telemart.Client.Helpers;

namespace Telemart.Client.Extensions
{
    public static class DateTimeExtentions
    {
        public static DateDiff Diff(this DateTime fromDate, DateTime toDate)
        {
            DateDiff dateDiff;

            if ((toDate - fromDate).Days < 0)
            {
                throw new ArgumentException("fromDate must be earlier than toDate");
            }

            if ((toDate - fromDate).Days > 0)
            {
                int daysInfromDateMonth = DateTime.DaysInMonth(fromDate.Year, fromDate.Month);
                int daysRemain = toDate.Day + (daysInfromDateMonth - fromDate.Day);
                int totalDay = ((daysRemain % daysInfromDateMonth) + daysInfromDateMonth) % daysInfromDateMonth;

                if (toDate.Month > fromDate.Month)
                {
                    dateDiff = new DateDiff(
                        toDate.Year - fromDate.Year,
                        toDate.Month - (fromDate.Month + 1) + (daysRemain / daysInfromDateMonth),
                        totalDay);
                }
                else if (toDate.Month == fromDate.Month)
                {
                    if (toDate.Day >= fromDate.Day)
                    {
                        dateDiff = new DateDiff(
                            toDate.Year - fromDate.Year,
                            0,
                            toDate.Day - fromDate.Day);
                    }
                    else
                    {
                        dateDiff = new DateDiff(
                            (toDate.Year - 1) - fromDate.Year,
                            11,
                            daysInfromDateMonth - (fromDate.Day - toDate.Day));
                    }
                }
                else
                {
                    dateDiff = new DateDiff(
                        (toDate.Year - 1) - fromDate.Year,
                        toDate.Month + (11 - fromDate.Month) + (daysRemain / daysInfromDateMonth),
                        totalDay);
                }
            }
            else
            {
                dateDiff = new DateDiff(0, 0, 0);
            }

            return dateDiff;
        }
    }

    public class DateDiff
    {
        private readonly string[] daysWords;
        private readonly string[] monthsWords;
        private readonly string[] yearsWords;

        public DateDiff(int years, int months, int days)
        {
            Years = years;
            Months = months;
            Days = days;

            daysWords = new[] { "день", "дня", "дней" };
            monthsWords = new[] { "месяц", "месяца", "месяцев" };
            yearsWords = new[] { "год", "года", "лет" };
        }

        public int Years { get; }

        public int Months { get; }

        public int Days { get; }

        public string GetFormatted(string separator = ", ")
        {
            List<string> dateParts = new List<string>();

            if (Years != 0)
            {
                dateParts.Add($"{Years} {WordEndingHelper.GetWordByNumber(Years, yearsWords)}");
            }

            if (Months != 0)
            {
                dateParts.Add($"{Months} {WordEndingHelper.GetWordByNumber(Months, monthsWords)}");
            }

            if (Days != 0)
            {
                dateParts.Add($"{Days} {WordEndingHelper.GetWordByNumber(Days, daysWords)}");
            }

            if (!dateParts.Any())
            {
                dateParts.Add($"{Days} {daysWords[2]}");
            }

            return string.Join(separator, dateParts);
        }
    }
}
