using Telemart.Client.Common.Controls;
using Xunit;

namespace Telemart.Client.Tests.Control
{
    public class IbanTextEditTests
    {
        [WpfTheory]
        [InlineData(" UA58322001000026207316455868")]
        [InlineData("00000000000000000000000000000")]
        [InlineData("11111111111111111111111111111")]
        [InlineData("11111111111111111111111111112")]
        [InlineData("22222222222222222222222222222")]
        public void NotValidTest(string iban)
        {
            IbanTextEdit textEdit = new IbanTextEdit();
            textEdit.EditValue = iban;
            textEdit.DoValidate();
            Assert.True(textEdit.HasValidationError, textEdit.ValidationError?.ErrorContent?.ToString());
        }

        [WpfTheory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("    ")]
        [InlineData("UA983510050000026207810104039")]
        public void ValidTest(string iban)
        {
            IbanTextEdit textEdit = new IbanTextEdit();
            textEdit.EditValue = iban;
            textEdit.DoValidate();
            Assert.False(textEdit.HasValidationError, textEdit.ValidationError?.ErrorContent?.ToString());
        }
    }
}