using Telemart.Client.Business;
using Xunit;

//// ReSharper disable ExceptionNotDocumented

namespace Telemart.Client.Tests
{
    public class ProductBarcodeTests
    {
        [Fact]
        public void LeadingZeroBarcodeTest()
        {
            ProductBarcode barcode = new ProductBarcode("XYZ123456789");

            Assert.False(barcode.IsOur);
            Assert.Null(barcode.OurProductId);
            Assert.Equal("0XYZ123456789", barcode.Barcode);
            Assert.True(barcode.IsValid);
            Assert.Empty(barcode.Errors);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("    ")]
        public void NullOrWhitespaceValueTest(string barcodeText)
        {
            ProductBarcode barcode = new ProductBarcode(barcodeText);

            Assert.False(barcode.IsOur);
            Assert.Null(barcode.OurProductId);
            Assert.Equal(barcodeText, barcode.Barcode);
            Assert.False(barcode.IsValid);
            Assert.NotEmpty(barcode.Errors);
        }

        [Fact]
        public void OurBarcodeTest()
        {
            int expectedProductId = 45367;
            string barcodeText = $"TEL-{expectedProductId}";

            ProductBarcode barcode = new ProductBarcode(barcodeText);

            Assert.True(barcode.IsOur);
            Assert.Equal(expectedProductId, barcode.OurProductId);
            Assert.Equal(barcodeText, barcode.Barcode);
            Assert.True(barcode.IsValid);
            Assert.Empty(barcode.Errors);
        }
    }
}