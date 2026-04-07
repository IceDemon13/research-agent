using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.HotlineCompetitor;

namespace Telemart.Client.Data.Requests.Features.HotlineCompetitor
{
    public class UpdateHotlineCompetitor : UpdateEntityResultRequestBase<HotlineCompetitorDto, HotlineCompetitorSaveDto>
    {
        public UpdateHotlineCompetitor(int id, HotlineCompetitorSaveDto dto)
            : base(dto, ApiResources.HotlineCompetitors, id)
        {
        }
    }
}
