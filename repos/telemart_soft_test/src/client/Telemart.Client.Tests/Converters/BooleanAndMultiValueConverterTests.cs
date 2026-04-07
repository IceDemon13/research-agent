using System.Globalization;
using Telemart.Client.Common.Converters;
using Xunit;

//// ReSharper disable ExceptionNotDocumented

namespace Telemart.Client.Tests.Converters
{
    public sealed class BooleanAndMultiValueConverterTests
    {
        [Fact]
        public void FailureTest()
        {
            BooleanAndMultiValueConverter converter = new BooleanAndMultiValueConverter();

            object[] values = { true, true, true, false };

            bool result = (bool)converter.Convert(values, typeof(object), null, CultureInfo.CurrentCulture);
            Assert.False(result);

            converter.Inverse = true;
            result = (bool)converter.Convert(values, typeof(object), null, CultureInfo.CurrentCulture);
            Assert.True(result);
        }

        [Fact]
        public void SuccessTest()
        {
            BooleanAndMultiValueConverter converter = new BooleanAndMultiValueConverter();

            object[] values = { true, true, true, true };

            bool result = (bool)converter.Convert(values, typeof(object), null, CultureInfo.CurrentCulture);
            Assert.True(result);

            converter.Inverse = true;
            result = (bool)converter.Convert(values, typeof(object), null, CultureInfo.CurrentCulture);
            Assert.False(result);
        }

        [Fact]
        public void ValuesEmptyTest()
        {
            BooleanAndMultiValueConverter converter = new BooleanAndMultiValueConverter();

            bool result = (bool)converter.Convert(new object[0], typeof(object), null, CultureInfo.CurrentCulture);

            Assert.False(result);
        }

        [Fact]
        public void ValuesNullTest()
        {
            BooleanAndMultiValueConverter converter = new BooleanAndMultiValueConverter();

            bool result = (bool)converter.Convert(null, typeof(object), null, CultureInfo.CurrentCulture);

            Assert.False(result);
        }
    }
}