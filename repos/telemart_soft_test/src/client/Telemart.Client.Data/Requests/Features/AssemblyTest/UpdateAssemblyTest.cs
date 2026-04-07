using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.AssemblyTest
{
    public class UpdateAssemblyTest : UpdateEntityResultRequestBase<AssemblyTestDto, AssemblyTestSaveDto>
    {
        public UpdateAssemblyTest(AssemblyTestSaveDto dto)
            : base(dto, ApiResources.AssemblyTests, dto.Id)
        {
        }
    }
}