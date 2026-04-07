using Telemart.Client.Business.Barcode;
using Xunit;

//// ReSharper disable ExceptionNotDocumented

namespace Telemart.Client.Tests
{
    public class OurServiceBarcodeTests
    {
        [Theory]
        [InlineData(null, false, -1)]
        [InlineData("", false, -1)]
        [InlineData("    ", false, -1)]
        [InlineData("SR3412", false, -1)]
        [InlineData("SR-text", false, -1)]
        [InlineData("SR-1", false, -1)]
        [InlineData("SR-11", false, -1)]
        [InlineData("SR-100", false, -1)]
        [InlineData("SR-0999", false, -1)]
        [InlineData("SR-1000", true, 1000)]
        [InlineData("SR-16000", true, 16000)]
        [InlineData("SR-234567", true, 234567)]
        [InlineData("SR-1112223", false, -1)]
        public void RecognizeBarcodeTest(string barcodeText, bool valid, int serviceRequestId)
        {
            OurServiceBarcode barcode = new OurServiceBarcode(barcodeText);

            Assert.Equal(barcodeText, barcode.BarcodeText);
            Assert.Equal(valid, barcode.IsValid);
            Assert.Equal(serviceRequestId, barcode.ServiceRequestId);
        }
    }
}