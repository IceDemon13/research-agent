using Newtonsoft.Json;
using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects.Novaposhta;

namespace Telemart.Client.Data.Requests.Features.Novaposhta.Actions
{
    public sealed class CreateNpTtnByServiceInvoice : CallActionWithBodyRequestResultBase<NpDocumentDto, CreateNpTtnByServiceInvoice.NovaposhtaTtnCreateByServiceInvoiceDto>
    {
        public CreateNpTtnByServiceInvoice(int serviceMovementId, int packagePlaces, double packageWeight, decimal packageInsurance, bool addToApplication, int? packageWidth, int? packageLength, int? packageHeight, bool? createOnLegal)
            : base(new NovaposhtaTtnCreateByServiceInvoiceDto(serviceMovementId, packageWeight, packagePlaces, packageInsurance, addToApplication, packageWidth, packageLength, packageHeight, createOnLegal), ApiResources.NovaposhtaDocuments, "create_by_service_invoice")
        {
        }

        public sealed class NovaposhtaTtnCreateByServiceInvoiceDto
        {
            public NovaposhtaTtnCreateByServiceInvoiceDto(int idServiceMovement, double weight, int places, decimal insurance, bool addToApplication, int? packageWidth, int? packageLength, int? packageHeight, bool? createOnLegal)
            {
                Id = idServiceMovement;
                PackageWeight = weight;
                PackagePlaces = places;
                PackageInsurance = insurance;
                AddToApplication = addToApplication;
                Width = packageWidth;
                Length = packageLength;
                Height = packageHeight;
                CreateOnLegal = createOnLegal;
            }

            [JsonProperty("id")]
            public int Id { get; set; }

            [JsonProperty("package_insurance")]
            public decimal PackageInsurance { get; set; }

            [JsonProperty("package_places")]
            public int PackagePlaces { get; set; }

            [JsonProperty("package_weight")]
            public double PackageWeight { get; set; }

            [JsonProperty("add_to_application")]
            public bool AddToApplication { get; set; }

            [JsonProperty("package_width")]
            public int? Width { get; set; }

            [JsonProperty("package_length")]
            public int? Length { get; set; }

            [JsonProperty("package_height")]
            public int? Height { get; set; }

            [JsonProperty("create_on_legal")]
            public bool? CreateOnLegal { get; set; }
        }
    }
}