using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.Paging;
using Telemart.Client.TransferObjects.Showcase;

namespace Telemart.Client.Data.Requests.Features.Showcase
{
    public sealed class UpdateAutoShowcases : UpdateEntityRequestBase<PagedResult<AutoShowcaseDto>, AutoShowcasesSaveDto>
    {
        public UpdateAutoShowcases(AutoShowcasesSaveDto dto)
            : base(dto, ApiResources.Showcases, "auto_save")
        {
        }
    }
}