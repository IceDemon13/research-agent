using System.Threading.Tasks;
using DevExpress.XtraReports;
using DevExpress.XtraReports.UI;
using Telemart.Client.Dictionaries;
using Telemart.Client.Reports.ServiceRequest;
using Telemart.Client.ViewModels.Service.ServiceRequests;

namespace Telemart.Client.Business.ServiceRequest
{
    public class ServiceRequestReportBuilder : IServiceRequestReportBuilder
    {
        public async Task<IReport> BuildServiceRequestReportAsync(ServiceRequestViewItem serviceRequest, bool printExtention)
        {
            XtraReport report;
            if (serviceRequest.Requirement == ServiceRequestRequirement.Repair &&
                serviceRequest.ServiceRepairTypeId == ServiceRepairType.Paid.Id)
            {
                report = new ServiceRequestPaidRepairReport
                {
                    DataSource = new[]
                    {
                        new ServiceRequestReportData(serviceRequest, printExtention)
                    }
                };
            }
            else
            {
                report = new ServiceRequestReport
                {
                    DataSource = new[]
                    {
                        new ServiceRequestReportData(serviceRequest, printExtention)
                    }
                };
            }

            await report.CreateDocumentAsync();

            return report;
        }
    }
}