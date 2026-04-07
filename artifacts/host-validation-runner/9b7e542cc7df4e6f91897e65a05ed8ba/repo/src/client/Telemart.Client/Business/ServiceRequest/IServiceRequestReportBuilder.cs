using System.Threading.Tasks;
using DevExpress.XtraReports;
using Telemart.Client.ViewModels.Service.ServiceRequests;

namespace Telemart.Client.Business.ServiceRequest
{
    public interface IServiceRequestReportBuilder
    {
        Task<IReport> BuildServiceRequestReportAsync(ServiceRequestViewItem serviceRequest, bool printExtention);
    }
}