using Newtonsoft.Json;
using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects.Novaposhta;

namespace Telemart.Client.Data.Requests.Features.Novaposhta.Actions
{
    public class CreateNpTtnByReturnInvoice : CallActionWithBodyRequestResultBase<NpDocumentDto, CreateNpTtnByReturnInvoice.NovaposhtaTtnCreateByReturnInvoiceDto>
    {
        public CreateNpTtnByReturnInvoice(int returnInvoiceId, int packagePlaces, decimal packageWeight, bool addToApplication)
            : base(new NovaposhtaTtnCreateByReturnInvoiceDto(returnInvoiceId, packageWeight, packagePlaces, addToApplication), ApiResources.NovaposhtaDocuments, "create_by_return_invoice")
        {
        }

        public class NovaposhtaTtnCreateByReturnInvoiceDto
        {
            public NovaposhtaTtnCreateByReturnInvoiceDto(int id, decimal weight, int places, bool addToApplication)
            {
                Id = id;
                Weight = weight;
                Places = places;
                AddToApplication = addToApplication;
            }

            [JsonProperty("id")]
            public int Id { get; set; }

            [JsonProperty("weight")]
            public decimal Weight { get; set; }

            [JsonProperty("places")]
            public int Places { get; set; }

            [JsonProperty("add_to_application")]
            public bool AddToApplication { get; set; }
        }
    }
}
