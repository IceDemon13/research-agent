using System.Collections.Generic;
using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.AssemblyService;

namespace Telemart.Client.Data.Requests.Features.AssemblyService
{
    public class UpdateAssemblyService : UpdateEntityResultRequestBase<AssemblyServiceDto, AssemblyServiceSaveDto>
    {
        public UpdateAssemblyService(int id, int employeeId, IReadOnlyCollection<AssemblyServiceProductSaveDto> products)
            : base(new AssemblyServiceSaveDto(id, employeeId, products), ApiResources.AssemblyService, id)
        {
        }
    }
}