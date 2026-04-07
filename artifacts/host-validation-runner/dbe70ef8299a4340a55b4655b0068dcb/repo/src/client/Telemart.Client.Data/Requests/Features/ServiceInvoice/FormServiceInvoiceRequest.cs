using System;
using Newtonsoft.Json;
using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.ServiceInvoice
{
    public sealed class FormServiceInvoiceRequest : CallEntityActionWithBodyRequestResultBase<ServiceInvoiceDto, FormServiceInvoiceRequest.ServiceInvoiceFormDto>
    {
        public FormServiceInvoiceRequest(int serviceInvoiceId, int[] serviceRequestIds)
            : base(serviceInvoiceId, new ServiceInvoiceFormDto { ServiceRepairIds = serviceRequestIds }, ApiResources.ServiceInvoices, "form")
        {
            if (serviceRequestIds == null)
            {
                throw new ArgumentNullException(nameof(serviceRequestIds));
            }

            if (serviceRequestIds.Length == 0)
            {
                throw new ArgumentException("Sequence contains no elements", nameof(serviceRequestIds));
            }
        }

        public class ServiceInvoiceFormDto
        {
            [JsonProperty("service_repair_ids")]
            public int[] ServiceRepairIds { get; set; }
        }
    }
}