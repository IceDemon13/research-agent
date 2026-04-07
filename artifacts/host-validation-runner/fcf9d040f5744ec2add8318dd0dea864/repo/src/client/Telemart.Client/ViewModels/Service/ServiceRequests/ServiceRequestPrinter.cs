using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DevExpress.Xpf.Printing;
using DevExpress.XtraReports;
using Telemart.Client.Business.ServiceRequest;
using Telemart.Client.Common;
using Telemart.Client.Data.Requests.Features.Catalog;
using Telemart.Client.Data.Requests.Features.Category;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.ViewModels.Service.ServiceRequests
{
    internal sealed class ServiceRequestPrinter : IServiceRequestPrinter
    {
        private readonly IWebClient webClient;
        private readonly IServiceRequestReportBuilder reportBuilder;

        public ServiceRequestPrinter(
            IServiceRequestReportBuilder serviceRequestReportBuilder,
            IWebClient webClient)
        {
            this.webClient = webClient;
            reportBuilder = serviceRequestReportBuilder;
        }

        public async Task PrintAsync(ServiceRequestViewItem serviceRequest)
        {
            List<CategoryDto> categories = await webClient.ExecuteApiRequestAsync(new QueryCategories(), true).GetPagedResultDataAsync();

            List<ProductDto> products = await webClient.ExecuteCatalogApiRequestAsync(new QueryProductByIds(Constants.TelemartContractorId, new[] { serviceRequest.ProductId }));

            CategoryDto category = categories.FirstOrDefault(x => products.First().CategoryId == x.Id);

            bool printExtention = category.TypeId == CategoryType.PcComponents.Id && serviceRequest.Requirement == ServiceRequestRequirement.Repair;

            IReport report = await reportBuilder.BuildServiceRequestReportAsync(serviceRequest, printExtention);

            PrintHelper.ShowPrintPreviewDialog(App.Current.MainWindow, report);
        }
    }
}