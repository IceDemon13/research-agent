using System.Collections.Generic;
using Newtonsoft.Json;
using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects;
using static Telemart.Client.Data.Requests.Features.AssemblyService.Actions.SetTestResults;

namespace Telemart.Client.Data.Requests.Features.AssemblyService.Actions
{
    public sealed class SetTestResults : CallEntityActionWithBodyRequestResultBase<List<AssemblyTestResultDto>, AssemblyTestResultUpdateDto>
    {
        public SetTestResults(List<AssemblyTestResultDto> results, int assemblyServiceId)
            : base(assemblyServiceId, new AssemblyTestResultUpdateDto(results), ApiResources.AssemblyService, "set_test_results")
        {
        }

        public class AssemblyTestResultUpdateDto
        {
            public AssemblyTestResultUpdateDto(List<AssemblyTestResultDto> results)
            {
                Results = results;
            }

            [JsonProperty("results")]
            public List<AssemblyTestResultDto> Results { get; set; }
        }
    }
}