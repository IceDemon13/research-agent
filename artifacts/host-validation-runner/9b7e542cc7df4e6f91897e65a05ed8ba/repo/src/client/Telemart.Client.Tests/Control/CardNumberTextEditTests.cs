using System;
using Telemart.Client.Common.Controls;
using Xunit;

namespace Telemart.Client.Tests.Control
{
    public class CardNumberTextEditTests
    {
        [WpfTheory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("    ")]
        [InlineData("4029610033590774")]
        [InlineData("0000000000000000")]
        public void ValidTest(string cardNumber)
        {
            CardNumberTextEdit textEdit = new CardNumberTextEdit();
            textEdit.EditValue = cardNumber;
            textEdit.DoValidate();
            Assert.False(textEdit.HasValidationError, textEdit.ValidationError?.ErrorContent?.ToString());
        }

        [WpfTheory]
        [InlineData("1111111111111111")]
        [InlineData("222222222")]
        [InlineData("2222222222222222")]
        public void NotValidTest(string inn)
        {
            CardNumberTextEdit textEdit = new CardNumberTextEdit();
            textEdit.EditValue = inn;
            textEdit.DoValidate();
            Assert.True(textEdit.HasValidationError, textEdit.ValidationError?.ErrorContent?.ToString());
        }
    }
}