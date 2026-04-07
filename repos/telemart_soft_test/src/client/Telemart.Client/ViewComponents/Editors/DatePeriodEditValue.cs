using System;

namespace Telemart.Client.ViewComponents.Editors
{
    public sealed class DatePeriodEditValue : IEquatable<DatePeriodEditValue>
    {
        private const string CurrentMonthName = "ТМ";
        private const string CurrentWeekName = "ТН";
        private const string CurrentYearName = "ТГ";
        private const string LastMonthName = "ПМ";
        private const string LastWeekName = "ПН";
        private const string LastYearName = "ПГ";
        private const string NoneName = "ПП";
        private const string EmptyName = "БП";

        private readonly Func<(DateTime From, DateTime Till)> getPeriod;

        private DatePeriodEditValue(string name, string description, Func<(DateTime From, DateTime Till)> getPeriod)
        {
            this.getPeriod = getPeriod;
            Name = name;
            Description = description;
        }

        public static DatePeriodEditValue CurrentMonth { get; } = new DatePeriodEditValue(CurrentMonthName, "Текущий месяц", GetCurrentMonthDateTimePeriod);

        public static DatePeriodEditValue CurrentWeek { get; } = new DatePeriodEditValue(CurrentWeekName, "Текущая неделя", GetCurrentWeekDateTimePeriod);

        public static DatePeriodEditValue CurrentYear { get; } = new DatePeriodEditValue(CurrentYearName, "Текущий год", GetCurrentYearDateTimePeriod);

        public static DatePeriodEditValue LastMonth { get; } = new DatePeriodEditValue(LastMonthName, "Прошлый месяц", GetLastMonthDateTimePeriod);

        public static DatePeriodEditValue LastWeek { get; } = new DatePeriodEditValue(LastWeekName, "Прошлая неделя", GetLastWeekDateTimePeriod);

        public static DatePeriodEditValue LastYear { get; } = new DatePeriodEditValue(LastYearName, "Прошлый год", GetLastYearDateTimePeriod);

        public static DatePeriodEditValue None { get; } = new DatePeriodEditValue(NoneName, "Произвольный период", null);

        public static DatePeriodEditValue Empty { get; } = new DatePeriodEditValue(EmptyName, "Без периода", null);

        public string Name { get; }

        public string Description { get; }

        public static bool operator ==(DatePeriodEditValue left, DatePeriodEditValue right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(DatePeriodEditValue left, DatePeriodEditValue right)
        {
            return !Equals(left, right);
        }

        public static DatePeriodEditValue GetByName(string period)
        {
            DatePeriodEditValue value;

            switch (period)
            {
                case CurrentWeekName:
                    value = CurrentWeek;
                    break;
                case LastWeekName:
                    value = LastWeek;
                    break;
                case CurrentMonthName:
                    value = CurrentMonth;
                    break;
                case LastMonthName:
                    value = LastMonth;
                    break;
                case CurrentYearName:
                    value = CurrentYear;
                    break;
                case LastYearName:
                    value = LastYear;
                    break;
                case NoneName:
                    value = None;
                    break;
                case EmptyName:
                    value = Empty;
                    break;
                default:
                    throw new NotSupportedException();
            }

            return value;
        }

        public (DateTime From, DateTime Till) GetDateTimePeriod()
        {
            return getPeriod();
        }

        public bool Equals(DatePeriodEditValue other)
        {
            if (ReferenceEquals(null, other))
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return string.Equals(Name, other.Name, StringComparison.OrdinalIgnoreCase);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }

            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            return obj is DatePeriodEditValue value && Equals(value);
        }

        public override int GetHashCode()
        {
            return StringComparer.OrdinalIgnoreCase.GetHashCode(Name);
        }

        private static (DateTime From, DateTime Till) GetCurrentWeekDateTimePeriod()
        {
            DateTime today = DateTime.Today;
            DateTime f = today.AddDays(-1 * ((int)today.DayOfWeek - 1));
            DateTime t = f.AddDays(6);
            return (f, t);
        }

        private static (DateTime From, DateTime Till) GetLastWeekDateTimePeriod()
        {
            DateTime today = DateTime.Today;
            DateTime f = today.AddDays(-1 * ((int)today.DayOfWeek + 6));
            DateTime t = f.AddDays(6);
            return (f, t);
        }

        private static (DateTime From, DateTime Till) GetCurrentMonthDateTimePeriod()
        {
            DateTime today = DateTime.Today;
            DateTime f = today.AddDays(-today.Day + 1);
            DateTime t = f.AddDays(DateTime.DaysInMonth(today.Year, today.Month) - 1);
            return (f, t);
        }

        private static (DateTime From, DateTime Till) GetLastMonthDateTimePeriod()
        {
            DateTime today = DateTime.Today;
            DateTime lastMonth = today.AddMonths(-1);
            DateTime f = lastMonth.AddDays(-lastMonth.Day + 1);
            DateTime t = f.AddDays(DateTime.DaysInMonth(lastMonth.Year, lastMonth.Month) - 1);
            return (f, t);
        }

        private static (DateTime From, DateTime Till) GetCurrentYearDateTimePeriod()
        {
            DateTime today = DateTime.Today;
            DateTime f = today.AddDays(-today.DayOfYear + 1);
            DateTime t = f.AddMonths(11).AddDays(DateTime.DaysInMonth(f.Year, 12) - 1);
            return (f, t);
        }

        private static (DateTime From, DateTime Till) GetLastYearDateTimePeriod()
        {
            DateTime today = DateTime.Today;
            DateTime lastYear = today.AddYears(-1);
            DateTime f = lastYear.AddDays(-lastYear.DayOfYear + 1);
            DateTime t = f.AddMonths(11).AddDays(DateTime.DaysInMonth(f.Year, 12) - 1);
            return (f, t);
        }
    }
}