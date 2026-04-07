using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.Data.Requests.Features.Area
{
    public sealed class UpdateArea : UpdateEntityRequestBase<Result<AreaDto>, AreaDto>
    {
        public UpdateArea(int id, AreaDto dto)
            : base(dto, ApiResources.Areas, id)
        {
        }
    }
}