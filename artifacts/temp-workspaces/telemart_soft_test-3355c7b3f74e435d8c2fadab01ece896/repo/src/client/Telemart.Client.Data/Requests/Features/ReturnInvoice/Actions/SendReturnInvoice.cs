using Newtonsoft.Json;
using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects.ReturnInvoice;

namespace Telemart.Client.Data.Requests.Features.ReturnInvoice.Actions
{
    public class SendReturnInvoice : CallEntityActionWithBodyRequestResultBase<ReturnInvoiceDto, SendReturnInvoice.SendReturnInvoiceDto>
    {
        public SendReturnInvoice(int id)
            : base(id, new SendReturnInvoiceDto(id), ApiResources.ReturnInvoices, "send")
        {
        }

        public class SendReturnInvoiceDto
        {
            public SendReturnInvoiceDto(int id)
            {
                Id = id;
            }

            [JsonProperty("id")]
            public int Id { get; set; }
        }
    }
}
