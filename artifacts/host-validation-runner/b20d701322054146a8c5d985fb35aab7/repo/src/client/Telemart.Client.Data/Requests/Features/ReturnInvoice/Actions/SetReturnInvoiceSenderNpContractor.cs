using Newtonsoft.Json;
using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects.ReturnInvoice;

namespace Telemart.Client.Data.Requests.Features.ReturnInvoice.Actions
{
    public class SetReturnInvoiceSenderNpContractor : CallEntityActionWithBodyRequestResultBase<ReturnInvoiceDto, SetReturnInvoiceSenderNpContractor.SetReturnInvoiceSenderNpContractorDto>
    {
        public SetReturnInvoiceSenderNpContractor(int id, string npContractorDto)
            : base(id, new SetReturnInvoiceSenderNpContractorDto(id, npContractorDto), ApiResources.ReturnInvoices, "set_sender_np_contractor")
        {
        }

        public class SetReturnInvoiceSenderNpContractorDto
        {
            public SetReturnInvoiceSenderNpContractorDto(int id, string senderNpContractorRef)
            {
                Id = id;
                SenderNpContractorRef = senderNpContractorRef;
            }

            [JsonProperty("id")]
            public int Id { get; set; }

            [JsonProperty("sender_np_contractor_ref")]
            public string SenderNpContractorRef { get; set; }
        }
    }
}
