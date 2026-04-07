using Newtonsoft.Json;
using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects.Novaposhta;
using Telemart.Client.TransferObjects.ServiceMovement;

namespace Telemart.Client.Data.Requests.Features.Novaposhta.Actions
{
    public sealed class CreateNpTtnByServiceMovement : CallActionWithBodyRequestResultBase<ServiceMovementNpDocumentDto, CreateNpTtnByServiceMovement.NovaposhtaTtnCreateByServiceMovementDto>
    {
        public CreateNpTtnByServiceMovement(int serviceMovementId, int packagePlaces, double packageWeight, decimal packageInsurance, bool addToApplication)
            : base(new NovaposhtaTtnCreateByServiceMovementDto(serviceMovementId, packageWeight, packagePlaces, packageInsurance, addToApplication), ApiResources.NovaposhtaDocuments, "create_by_service_movement")
        {
        }

        public sealed class NovaposhtaTtnCreateByServiceMovementDto
        {
            public NovaposhtaTtnCreateByServiceMovementDto(int idServiceMovement, double weight, int places, decimal insurance, bool addToApplication)
            {
                Id = idServiceMovement;
                PackageWeight = weight;
                PackagePlaces = places;
                PackageInsurance = insurance;
                AddToApplication = addToApplication;
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
        }
    }
}