using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.AssemblyTest
{
    public sealed class CreateAssemblyTestGroup : CreateEntityResultRequestBase<AssemblyTestGroupDto, AssemblyTestGroupCreateDto>
    {
        public CreateAssemblyTestGroup(AssemblyTestGroupCreateDto dto)
            : base(dto, ApiResources.AssemblyTests, "groups")
        {
        }
    }
}