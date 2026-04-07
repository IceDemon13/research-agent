using Newtonsoft.Json;
using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects.AssemblyService;

namespace Telemart.Client.Data.Requests.Features.AssemblyService.Actions
{
    public sealed class UpdateAssemblyServicePlaces : CallEntityActionWithBodyRequestResultBase<AssemblyServiceDto, UpdateAssemblyServicePlaces.UpdateAssemblyServicePlacesDto>
    {
        public UpdateAssemblyServicePlaces(int assemblyServiceId, int places)
            : base(assemblyServiceId, new UpdateAssemblyServicePlacesDto(assemblyServiceId, places), ApiResources.AssemblyService, "update_places")
        {
        }

        public class UpdateAssemblyServicePlacesDto
        {
            public UpdateAssemblyServicePlacesDto(int id, int places)
            {
                Id = id;
                Places = places;
            }

            [JsonProperty("id")]
            public int Id { get; init; }

            [JsonProperty("places")]
            public int Places { get; init; }
        }
    }
}
