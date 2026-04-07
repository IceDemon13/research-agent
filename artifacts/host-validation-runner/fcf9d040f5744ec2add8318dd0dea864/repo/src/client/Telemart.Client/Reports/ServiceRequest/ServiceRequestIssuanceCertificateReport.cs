using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Telemart.Client.Core.IO;
using Telemart.Client.Data.Requests.Features.Order;
using Telemart.Client.Data.Requests.Features.ServiceInvoice;
using Telemart.Client.Data.WebClient;
using Telemart.Client.TransferObjects;
using Telemart.Client.ViewModels.Service.ServiceRepairs;
using Telemart.Client.ViewModels.Service.ServiceRequests;

namespace Telemart.Client.Reports.ServiceRequest
{
    public static class ServiceRequestIssuanceCertificateReport
    {
        public static async Task PrintAsync(
            ServiceRequestViewItem viewItem,
            ServiceRepairViewItem serviceRepairViewItem,
            ServiceCenterDto serviceCenter,
            IWebClient webClient)
        {
            ServiceInvoiceDto serviceInvoice = null;

            if (serviceRepairViewItem?.ServiceInvoiceId.HasValue == true)
            {
                serviceInvoice = await webClient.ExecuteApiRequestAsync(new QueryServiceInvoice(serviceRepairViewItem.ServiceInvoiceId.Value));
            }

            OrderDto order = await webClient.ExecuteApiRequestAsync(new QueryOrder(viewItem.OrderId));

            string html = await GetHtmlAsync(viewItem, serviceRepairViewItem, serviceInvoice, serviceCenter, order.CustomerReceivedOn ?? order.CompletedOn.Value);

            await FileHelper.OpenAsFileAsync(Encoding.UTF8.GetBytes(html), "html");
        }

        private static async Task<string> GetHtmlAsync(
            ServiceRequestViewItem viewItem,
            ServiceRepairViewItem serviceRepairViewItem,
            ServiceInvoiceDto serviceInvoice,
            ServiceCenterDto serviceCenter,
            DateTime customerReceivedOn)
        {
            string htmlString = await File.ReadAllTextAsync(@"Reports\ServiceRequest\ServiceRequestIssuanceCertificateReport.html");

            StringBuilder htmlStringBuilder = new StringBuilder(htmlString);

            StringBuilder formattedHtml = htmlStringBuilder
                .Replace("{ServiceRequestId}", viewItem.Id.ToString())
                .Replace("{ServiceRequestActNumber}", "1")
                .Replace("{ProductName}", viewItem.ProductFullName ?? string.Empty)
                .Replace("{SerialNumber}", viewItem.SerialNumber ?? string.Empty)
                .Replace("{OrderCustomerReceivedOn}", customerReceivedOn.ToString("dd.MM.yyyy"))
                .Replace("{ProductAppearance}", viewItem.Appearance)
                .Replace("{ProductComplectation}", viewItem.CompletenessComment)
                .Replace("{CustomerDefect}", viewItem.StatedDefect)
                .Replace("{CustomerRequirement}", viewItem.Requirement.NameUkr)
                .Replace("{ServiceCenterIncomeDate}", serviceInvoice?.SendDate.HasValue == true ? serviceInvoice.SendDate.Value.ToString("dd.MM.yyyy") : string.Empty)
                .Replace("{ServiceCenterName}", serviceCenter?.Name ?? string.Empty)
                .Replace("{ServiceCenterConclusion}", serviceRepairViewItem?.ServiceCenterConclusion ?? string.Empty)
                .Replace("{ServiceRequestReadyOn}", viewItem.ReadyOn.HasValue ? viewItem.ReadyOn.Value.ToString("yyyy-MM-dd") : string.Empty)
                .Replace("{ServiceRequestComment}", viewItem.Comment)
                .Replace("{CurrentDate}", DateTime.Now.ToString("yyyy-MM-dd"))
                .Replace("{CustomerFIO}", viewItem.Fio)
                .Replace("{CustomerPhone}", viewItem.Phone)
                .Replace("{ResolutionId}", viewItem.RequirementResolution.Id.ToString())
                .Replace("{ServiceRequestReceivedOn}", (viewItem.ReceivedOn ?? DateTime.Now).ToString("yyyy-MM-dd"));

            return formattedHtml.ToString();
        }
    }
}
