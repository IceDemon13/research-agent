using System;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Telemart.Client.Common.Converters.Base;

namespace Telemart.Client.Common.Converters
{
    public sealed class EnumDescriptionConverter : ValueConverterBase
    {
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return GetEnumDescription((Enum)value);
        }

        private string GetEnumDescription(Enum enumObj)
        {
            FieldInfo fieldInfo = enumObj.GetType().GetField(enumObj.ToString());

            object[] attributes = fieldInfo?.GetCustomAttributes(typeof(DescriptionAttribute), false);

            string description;

            if (attributes?.Any() == true)
            {
                DescriptionAttribute attrib = (DescriptionAttribute)attributes[0];
                description = attrib.Description;
            }
            else
            {
                description = enumObj.ToString();
            }

            return description;
        }
    }
}