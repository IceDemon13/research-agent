using System.Globalization;
using Telemart.Client.Common.Converters;
using Xunit;

//// ReSharper disable ExceptionNotDocumented

namespace Telemart.Client.Tests.Converters
{
    public class StringToPhoneConverterTests
    {
        [Fact]
        public void ConvertNullTest()
        {
            StringToPhoneConverter converter = new StringToPhoneConverter();

            object res = converter.Convert(null, typeof(string), null, CultureInfo.CurrentCulture);

            Assert.Null(res);
        }

        [Fact]
        public void ConvertTest()
        {
            StringToPhoneConverter converter = new StringToPhoneConverter();

            string phone = "0975675306";
            string formattedPhone = converter.Convert(phone, typeof(string), null, CultureInfo.CurrentCulture)?.ToString();

            Assert.Equal("(097) 567-53-06", formattedPhone);
        }
    }
}