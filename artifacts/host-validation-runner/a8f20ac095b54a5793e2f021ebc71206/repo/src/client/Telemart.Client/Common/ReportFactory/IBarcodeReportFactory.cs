using System.Threading.Tasks;
using Telemart.Client.Reports.Product;

namespace Telemart.Client.Common.ReportFactory
{
    public interface IBarcodeReportFactory
    {
        Task<BarcodeReportFactoryResult> CreateAsync(BarcodeReportFormat barcodeReportFormat, string productName, int productId, int quantity);

        Task<BarcodeReportFactoryResult> CreateAsync(string productName, int productId, int quantity);
    }
}
