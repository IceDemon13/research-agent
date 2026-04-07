using System;

namespace Telemart.Client.Dictionaries
{
    public class DayOfWeek : DictionaryItem
    {
        public const int MondayId = 1;
        public const int TuesdayId = 2;
        public const int WednesdayId = 3;
        public const int ThursdayId = 4;
        public const int FridayId = 5;
        public const int SaturdayId = 6;
        public const int SundayId = 7;

        private DayOfWeek(int id, string name, string shortName)
            : base(id, name, true)
        {
            ShortName = shortName;
        }

        public static DayOfWeek Monday { get; } = new DayOfWeek(MondayId, "Понедельник", "Пн");

        public static DayOfWeek Tuesday { get; } = new DayOfWeek(TuesdayId, "Вторник", "Вт");

        public static DayOfWeek Wednesday { get; } = new DayOfWeek(WednesdayId, "Среда", "Ср");

        public static DayOfWeek Thursday { get; } = new DayOfWeek(ThursdayId, "Четверг", "Чт");

        public static DayOfWeek Friday { get; } = new DayOfWeek(FridayId, "Пятница", "Пт");

        public static DayOfWeek Saturday { get; } = new DayOfWeek(SaturdayId, "Суббота", "Сб");

        public static DayOfWeek Sunday { get; } = new DayOfWeek(SundayId, "Воскресенье", "Вс");

        public string ShortName { get; }

        public static DayOfWeek GetById(int id)
        {
            DayOfWeek dayOfWeek;
            switch (id)
            {
                case MondayId:
                    dayOfWeek = Monday;
                    break;
                case TuesdayId:
                    dayOfWeek = Tuesday;
                    break;
                case WednesdayId:
                    dayOfWeek = Wednesday;
                    break;
                case ThursdayId:
                    dayOfWeek = Thursday;
                    break;
                case FridayId:
                    dayOfWeek = Friday;
                    break;
                case SaturdayId:
                    dayOfWeek = Saturday;
                    break;
                case SundayId:
                    dayOfWeek = Sunday;
                    break;
                default:
                    throw new NotSupportedException();
            }

            return dayOfWeek;
        }
    }
}
