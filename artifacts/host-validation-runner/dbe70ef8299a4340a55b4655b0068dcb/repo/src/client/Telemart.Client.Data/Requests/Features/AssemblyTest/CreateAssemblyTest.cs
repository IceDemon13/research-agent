using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.AssemblyTest
{
    public sealed class CreateAssemblyTest : CreateEntityResultRequestBase<AssemblyTestDto, AssemblyTestCreateDto>
    {
        public CreateAssemblyTest(AssemblyTestCreateDto dto)
            : base(dto, ApiResources.AssemblyTests)
        {
        }
    }
}