using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DevExpress.XtraReports;
using MediatR;
using Telemart.Client.Common.Settings.Printing;
using Telemart.Client.Data.Requests.Features.Employee;
using Telemart.Client.Data.Requests.Features.Organization;
using Telemart.Client.Data.Requests.Features.ServiceCenter;
using Telemart.Client.Data.Requests.Features.ServiceInvoice;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries.Constants;
using Telemart.Client.Helpers;
using Telemart.Client.Mediator.Requests;
using Telemart.Client.Reports.ServiceInvoice;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Mediator.RequestsHandlers
{
    public sealed class PrintServiceInvoiceReportHandler : IRequestHandler<PrintServiceInvoiceReportRequest>
    {
        public PrintServiceInvoiceReportHandler(
            IWebClient webClient,
            IPrintingSettingsStore printingSettingsStore,
            IReportPrintHelper reportPrintHelper)
        {
            WebClient = webClient;
            PrintingSettingsStore = printingSettingsStore;
            ReportPrintHelper = reportPrintHelper;
        }

        private IWebClient WebClient { get; }

        private IPrintingSettingsStore PrintingSettingsStore { get; }

        private IReportPrintHelper ReportPrintHelper { get; }

        public async Task Handle(PrintServiceInvoiceReportRequest message, CancellationToken cancellationToken)
        {
            ServiceInvoiceDto serviceIvoice = await WebClient.ExecuteApiRequestAsync(new QueryServiceInvoice(message.ServiceInvoiceId));
            ServiceCenterDto serviceCenter = await WebClient.ExecuteApiRequestAsync(new QueryServiceCenter(serviceIvoice.ServiceCenterId));
            EmployeeDto employee = await WebClient.ExecuteApiRequestAsync(new QueryEmployee(serviceCenter.EmployeeId));
            OrganizationDto organization = await WebClient.ExecuteApiRequestAsync(new QueryOrganization(OrganizationConstants.DiscontMobileOrganizationId));

            IReadOnlyCollection<ServiceInvoiceProductReportData> reportProducts = serviceIvoice.Products
                .Select(x => new ServiceInvoiceProductReportData(x.ProductNameRu, x.ServiceRequestId, x.SerialNumber, x.Defect))
                .ToArray();

            ServiceInvoiceReportData reportData = new ServiceInvoiceReportData(reportProducts)
            {
                DateSend = serviceIvoice.SendDate!.Value,
                EmployeePhones = new[] { employee.Phone1, employee.Phone2 },
                EmployeeEmail = employee.Email,
                EmployeeName = employee.Name,
                Number = serviceIvoice.Id,
                OrganizationName = organization.Name,
                ServiceCenterName = serviceCenter.Name,
                ServiceCenterAddress = serviceCenter.Address
            };

            IReport report = new ServiceInvoiceReport { DataSource = new[] { reportData } };

            PrintingSettingsInfo printSettings = await PrintingSettingsStore.LoadAsync();

            ReportPrintHelper.Print(report, printSettings?.Main?.Name, printSettings?.Main?.PaperSource, true);
        }
    }
}