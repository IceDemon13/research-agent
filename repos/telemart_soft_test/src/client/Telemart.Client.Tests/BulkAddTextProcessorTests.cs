using System.Collections.Generic;
using System.Linq;
using System.Text;
using Telemart.Client.Helpers;
using Xunit;

//// ReSharper disable ExceptionNotDocumented

namespace Telemart.Client.Tests
{
    public class BulkAddTextProcessorTests
    {
        private readonly BulkAddTextProcessor processor = new BulkAddTextProcessor();

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("    ")]
        public void EmptyTextTest(string text)
        {
            IEnumerable<(string Pattern, int Quantity)> items = processor.HandleText(text);
            Assert.Empty(items);
        }

        [Fact]
        public void TextShouldBeParsedCorrectlyTest()
        {
            StringBuilder s = new StringBuilder();
            s.AppendLine("       ");
            s.AppendLine(" Huawei MediaPad T3 8.0 16GB LTE Gold   10 шт  ");
            s.AppendLine(" Huawei MediaPad T3 8.0 16GB LTE Gold   2 штуки");
            s.AppendLine("       ");
            s.AppendLine("LG M320 X Power 2 Black Blue\t23");
            s.AppendLine("LG M320 X Power 2 Black Blue   0");
            s.AppendLine("LG M320 X Power 2 Black Blue   -1");
            s.AppendLine("LG M320 X Power 2 Black Blue   not_valid_number");
            s.AppendLine("       ");
            s.AppendLine(" 2 Гб 1     2 шт.  ");
            s.AppendLine("2 Гб 1");

            (string Pattern, int Quantity)[] items = processor.HandleText(s.ToString()).ToArray();
            (string, int)[] extected = new[]
            {
                ("Huawei MediaPad T3 8.0 16GB LTE Gold", 10),
                ("Huawei MediaPad T3 8.0 16GB LTE Gold", 2),
                ("LG M320 X Power 2 Black Blue", 23),
                ("LG M320 X Power 2 Black Blue", 1),
                ("LG M320 X Power 2 Black Blue", 1),
                ("2 Гб 1", 2),
                ("2 Гб 1", 1)
            };

            Assert.Equal(extected, items);
        }
    }
}