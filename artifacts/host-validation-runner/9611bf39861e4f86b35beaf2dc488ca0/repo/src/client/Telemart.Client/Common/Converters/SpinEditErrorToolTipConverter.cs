using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Markup;
using DevExpress.Xpf.Editors;

namespace Telemart.Client.Common.Converters
{
    public class SpinEditErrorToolTipConverter : MarkupExtension, IValueConverter
    {
        private const string GeneralErrorText = "Значение вне диапазона";

        public string MinErrorText { get; set; } = GeneralErrorText;

        public string MaxErrorText { get; set; } = GeneralErrorText;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            SpinEdit se = value as SpinEdit;

            if (!int.TryParse(se.DisplayText, out int intValue))
            {
                return "Невалидное значение";
            }

            if (intValue < se.MinValue)
            {
                return MinErrorText;
            }

            return MaxErrorText;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value;
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return this;
        }
    }
}
