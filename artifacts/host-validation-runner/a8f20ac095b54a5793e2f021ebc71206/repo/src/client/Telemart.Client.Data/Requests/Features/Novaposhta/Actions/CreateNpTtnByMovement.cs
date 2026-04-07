using Newtonsoft.Json;
using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects.Novaposhta;

namespace Telemart.Client.Data.Requests.Features.Novaposhta.Actions
{
    public class CreateNpTtnByMovement : CallActionWithBodyRequestResultBase<NpDocumentDto, CreateNpTtnByMovement.NovaposhtaTtnCreateByMovementDto>
    {
        public CreateNpTtnByMovement(int movementId, int packagePlaces, double packageWeight, decimal packageInsurance, bool addToApplication)
            : base(new NovaposhtaTtnCreateByMovementDto(movementId, packageWeight, packagePlaces, packageInsurance, addToApplication), ApiResources.NovaposhtaDocuments, "create_by_movement")
        {
        }

        public class NovaposhtaTtnCreateByMovementDto
        {
            public NovaposhtaTtnCreateByMovementDto(int id, double weight, int places, decimal insurance, bool addToApplication)
            {
                Id = id;
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