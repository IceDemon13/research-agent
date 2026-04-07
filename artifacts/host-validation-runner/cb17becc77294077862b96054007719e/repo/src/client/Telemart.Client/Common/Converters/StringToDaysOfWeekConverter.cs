using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using DevExpress.Mvvm.Native;
using Telemart.Client.Common.Converters.Base;
using DayOfWeek = Telemart.Client.Dictionaries.DayOfWeek;

namespace Telemart.Client.Common.Converters
{
    public class StringToDaysOfWeekConverter : ValueConverterBase
    {
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string days = value as string;

            return days?.Select(CharToDayOfWeek)
                .Where(x => x != null)
                .Cast<object>()
                .ToList();
        }

        public override object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            List<object> daysObj = value as List<object>;

            int[] days = daysObj?.Cast<DayOfWeek>().Select(x => x.Id).OrderBy(x => x).ToArray();

            if (days == null)
            {
                return null;
            }

            return string.Join(string.Empty, days);
        }

        private DayOfWeek CharToDayOfWeek(char c)
        {
            int day = (int)char.GetNumericValue(c);
            if (day >= 1 && day <= 7)
            {
                return DayOfWeek.GetById(day);
            }

            return null;
        }
    }
}
