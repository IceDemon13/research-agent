using System;
using Telemart.Client.Core.Extensions;
using Xunit;

//// ReSharper disable ExceptionNotDocumented

namespace Telemart.Client.Tests
{
    public class StringExtensionsTests
    {
        [Fact]
        public void CompareWildcardDifferentStringsTest()
        {
            Assert.False("qwerty".CompareWildcard("sfgsdfg"));
            Assert.False("qwerty".CompareWildcard("qwerty  "));
            Assert.False("qwerty".CompareWildcard("  qwerty"));
        }

        [Fact]
        public void CompareWildcardEndTest()
        {
            Assert.True("qwerty".CompareWildcard("*ty"));
            Assert.True("qwerty".CompareWildcard("?werty"));
        }

        [Fact]
        public void CompareWildcardMiddleTest()
        {
            Assert.True("qwerty".CompareWildcard("qw*ty"));
            Assert.True("qwerty".CompareWildcard("*qw*t*"));

            Assert.True("galaxy s6".CompareWildcard("*S?6*"));
            Assert.True("galaxy s 6 black".CompareWildcard("*S?6*"));
            Assert.True("s-6".CompareWildcard("*S?6*"));
            Assert.True("s6".CompareWildcard("*S?6*"));
        }

        [Fact]
        public void CompareWildcardStartTest()
        {
            Assert.True("qwerty".CompareWildcard("qw*"));
            Assert.True("qwerty".CompareWildcard("qwert?"));
        }

        [Fact]
        public void CompareWildcardTheSameStringsTest()
        {
            Assert.True("qwerty".CompareWildcard("QWerTY"));
            Assert.True("qwerty".CompareWildcard("qwerty"));
        }

        [Fact]
        public void ContainsWindcardTest()
        {
            bool contains = "MADCATZ M.M.O. TE Gaming Mouse (MCB437140002/04/1)".ContainsWildcard("MCB437140002");
            Assert.True(contains);
        }

        [Theory]
        [InlineData("str1", "str2", "str1 (str2)")]
        [InlineData("str", "str", "str")]
        [InlineData("", "str2", "str2")]
        [InlineData("   ", "str2", "str2")]
        [InlineData(null, "str2", "str2")]
        [InlineData("str1", "", "str1")]
        [InlineData("str1", "    ", "str1")]
        [InlineData("str1", null, "str1")]
        [InlineData(null, null, "")]
        [InlineData(null, "", "")]
        [InlineData(null, "   ", "")]
        [InlineData("", null, "")]
        [InlineData("  ", null, "")]
        public void JoinSmartTest(string str1, string str2, string expected)
        {
            string actual = str1.JoinSmart(str2);

            Assert.Equal(expected, actual, StringComparer.Ordinal);
        }

        [Theory]
        [InlineData(null, null, null)]
        [InlineData("", null, "")]
        [InlineData(null, "", null)]
        [InlineData("aaaa", "bbbb", "aaaa")]
        [InlineData("aaaabbbb", "bb", "aaaabb")]
        public void TrimEndTest(string str1, string str2, string expected)
        {
            string actual = str1.TrimEnd(str2, StringComparison.Ordinal);

            Assert.Equal(expected, actual, StringComparer.Ordinal);
        }
    }
}