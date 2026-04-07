using System;
using System.Linq;
using Telemart.Client.ViewModels.Store;
using Xunit;

//// ReSharper disable ExceptionNotDocumented

namespace Telemart.Client.Tests
{
    public class SerialNumbersRangeTests
    {
        [Fact]
        public void FirstSerialShoudBeNotNull()
        {
            Assert.Throws<ArgumentNullException>(() => new SerialNumbersRange(null, "test"));
        }

        [Fact]
        public void SecondSerialShoudBeNotNull()
        {
            Assert.Throws<ArgumentNullException>(() => new SerialNumbersRange("test", null));
        }

        [Fact]
        public void SerialsLengthShouldBeEqual()
        {
            Assert.Throws<InvalidOperationException>(() => new SerialNumbersRange("test", "test1"));
        }

        [Theory]
        [InlineData("GCM0FM014464", "GCM0FM014468", new[] { "GCM0FM014464", "GCM0FM014465", "GCM0FM014466", "GCM0FM014467", "GCM0FM014468" })]
        [InlineData("2172159018298", "2172159018300", new[] { "2172159018298", "2172159018299", "2172159018300" })]
        [InlineData("00501", "00510", new[] { "00501", "00502", "00503", "00504", "00505", "00506", "00507", "00508", "00509", "00510" })]
        [InlineData("GCM0FM014468", "GCM0FM014464", new string[0])]
        [InlineData("GCM0FM014468", "GCM0FM014468", new string[0])]
        public void GenerateRangeSuccess(string sn1, string sn2, string[] expected)
        {
            SerialNumbersRange serialNumbersRange = new SerialNumbersRange(sn1, sn2);
            Assert.True(expected.SequenceEqual(serialNumbersRange.Range));
        }
    }
}