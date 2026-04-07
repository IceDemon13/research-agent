using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects.TradeIn;

namespace Telemart.Client.Data.Requests.Features.TradeIn.Actions
{
    public sealed class TradeInNomenclatureTask : CallEntityActionWithBodyRequestResultBase<TradeInDto, TradeInCreateNomenclatureTaskDto>
    {
        public TradeInNomenclatureTask(int id, TradeInCreateNomenclatureTaskDto dto)
            : base(id, dto, ApiResources.TradeIns, "nomenclature_task")
        {
        }
    }
}