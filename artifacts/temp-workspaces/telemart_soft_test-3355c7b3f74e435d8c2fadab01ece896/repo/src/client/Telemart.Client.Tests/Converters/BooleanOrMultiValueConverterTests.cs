using System.Globalization;
using Telemart.Client.Common.Converters;
using Xunit;

//// ReSharper disable ExceptionNotDocumented

namespace Telemart.Client.Tests.Converters
{
    public sealed class BooleanOrMultiValueConverterTests
    {
        [Fact]
        public void FailureTest()
        {
            BooleanOrMultiValueConverter converter = new BooleanOrMultiValueConverter();

            object[] values = { false, false, false, false };

            bool result = (bool)converter.Convert(values, typeof(object), null, CultureInfo.CurrentCulture);
            Assert.False(result);

            converter.Inverse = true;
            result = (bool)converter.Convert(values, typeof(object), null, CultureInfo.CurrentCulture);
            Assert.True(result);
        }

        [Fact]
        public void SuccessTest()
        {
            BooleanOrMultiValueConverter converter = new BooleanOrMultiValueConverter();

            object[] values = { false, false, true, false };

            bool result = (bool)converter.Convert(values, typeof(object), null, CultureInfo.CurrentCulture);
            Assert.True(result);

            converter.Inverse = true;
            result = (bool)converter.Convert(values, typeof(object), null, CultureInfo.CurrentCulture);
            Assert.False(result);
        }

        [Fact]
        public void ValuesEmptyTest()
        {
            BooleanOrMultiValueConverter converter = new BooleanOrMultiValueConverter();

            bool result = (bool)converter.Convert(new object[0], typeof(object), null, CultureInfo.CurrentCulture);

            Assert.False(result);
        }

        [Fact]
        public void ValuesNullTest()
        {
            BooleanOrMultiValueConverter converter = new BooleanOrMultiValueConverter();

            bool result = (bool)converter.Convert(null, typeof(object), null, CultureInfo.CurrentCulture);

            Assert.False(result);
        }
    }
}