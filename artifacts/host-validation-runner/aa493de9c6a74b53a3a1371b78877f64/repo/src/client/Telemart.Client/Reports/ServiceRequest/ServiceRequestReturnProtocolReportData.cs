using System.Globalization;
using Humanizer;

namespace Telemart.Client.Reports.ServiceRequest
{
    public class ServiceRequestReturnProtocolReportData
    {
        public ServiceRequestReturnProtocolReportData(int orderId, string productName, int productPrice)
        {
            OrderId = orderId;
            ProductName = productName;
            ProductPrice = productPrice;
            ProductPriceStr = productPrice.ToWords(GrammaticalGender.Feminine, new CultureInfo("uk-Ua"));
        }

        public int OrderId { get; }

        public string ProductName { get; }

        public int ProductPrice { get; }

        public string ProductPriceStr { get; }
    }
}
