using System.Collections.Generic;
using System.Threading.Tasks;
using DevExpress.XtraReports;
using Telemart.Client.ReportDesigner;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Business.Order
{
    public interface IOrderReportBuilder
    {
        Task<IReport> BuildAcceptanceProtocolReportAsync(OrderDto order, string contractorName, string cityName, bool todayAsIssueDate);

        Task<IReport> BuildChequeReportAsync(OrderDto order, int[] productIds, string contractorName, string cityName, bool todayAsIssueDate);

        Task<IReport> BuildTapeChequeReportAsync(OrderDto order, int[] productIds, string contractorName, string cityName, bool todayAsIssueDate);

        Task<IReport> BuildProductWarrantyCardReportAsync(OrderDto order, int productId, string serialNumber, IDictionary<int, string> warranties, bool todayAsIssueDate);

        Task<IReport> BuildWarrantyCardReportAsync(OrderDto order, int[] orderProductsIds, IDictionary<int, string> warranties, bool todayAsIssueDate, bool separateWarrantyCards);

        Task<IReport> BuildWarrantyCardReportAsync(
            OrderDto order,
            IReadOnlyCollection<OrderProductWarrantyCardReportData> products,
            bool todayAsIssueDate,
            bool separateWarrantyCards);
    }
}