using System;
using Telemart.Client.Common.Controls;
using Xunit;

namespace Telemart.Client.Tests.Control
{
    public class InnTextEditTests
    {
        [WpfTheory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("    ")]
        [InlineData("0000000000")]
        [InlineData("3927608078")]
        public void ValidTest(string inn)
        {
            InnTextEdit textEdit = new InnTextEdit();
            textEdit.EditValue = inn;
            textEdit.DoValidate();
            Assert.False(textEdit.HasValidationError, textEdit.ValidationError?.ErrorContent?.ToString());
        }

        [WpfTheory]
        [InlineData("dfg")]
        [InlineData("456456")]
        [InlineData("00e0000000")]
        [InlineData("3927708078")]
        public void NotValidTest(string inn)
        {
            InnTextEdit textEdit = new InnTextEdit();
            textEdit.EditValue = inn;
            textEdit.DoValidate();
            Assert.True(textEdit.HasValidationError, textEdit.ValidationError?.ErrorContent?.ToString());
        }
    }
}