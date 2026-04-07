using System;
using System.Globalization;
using DevExpress.Mvvm.DataAnnotations;
using Telemart.Client.Common.Converters.Base;
using Telemart.Client.Extensions;

namespace Telemart.Client.Common.Converters
{
    public sealed class EnumToImageSourceConverter : ValueConverterBase
    {
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            Enum enumVariable = value as Enum;
            return enumVariable?.GetAttributeOfType<ImageAttribute>()?.ImageUri;
        }
    }
}
