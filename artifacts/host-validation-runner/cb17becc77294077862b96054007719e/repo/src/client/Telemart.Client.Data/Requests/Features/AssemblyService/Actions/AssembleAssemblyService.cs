using Newtonsoft.Json;
using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects.AssemblyService;
using static Telemart.Client.Data.Requests.Features.AssemblyService.Actions.AssembleAssemblyService;

namespace Telemart.Client.Data.Requests.Features.AssemblyService.Actions
{
    public sealed class AssembleAssemblyService : CallEntityActionWithBodyRequestResultBase<AssemblyServiceDto, AssembleAssemblyServiceDto>
    {
        public AssembleAssemblyService(int assemblyServiceId, int places)
            : base(assemblyServiceId, new AssembleAssemblyServiceDto(places), ApiResources.AssemblyService, "assemble")
        {
        }

        public class AssembleAssemblyServiceDto
        {
            public AssembleAssemblyServiceDto(int places)
            {
                Places = places;
            }

            [JsonProperty("places")]
            public int Places { get; set; }
        }
    }
}