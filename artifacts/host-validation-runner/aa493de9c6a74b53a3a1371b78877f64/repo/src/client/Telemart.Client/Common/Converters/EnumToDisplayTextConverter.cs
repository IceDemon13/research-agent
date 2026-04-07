using System;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using Telemart.Client.Common.Converters.Base;
using Telemart.Client.Extensions;

namespace Telemart.Client.Common.Converters
{
    public sealed class EnumToDisplayTextConverter : ValueConverterBase
    {
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            Enum enumVariable = value as Enum;
            return enumVariable?.GetAttributeOfType<DisplayAttribute>()?.Name;
        }
    }
}