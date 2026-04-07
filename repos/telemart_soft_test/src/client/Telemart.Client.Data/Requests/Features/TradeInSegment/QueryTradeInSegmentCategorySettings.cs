using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.TradeInSegment;

namespace Telemart.Client.Data.Requests.Features.TradeInSegment
{
    public sealed class QueryTradeInSegmentCategorySettings : QueryEntitiesRequestBase<TradeInSegmentCategorySettingsDto>
    {
        public QueryTradeInSegmentCategorySettings()
            : base($"{ApiResources.TradeInSegments}/actions/category_settings")
        {
        }
    }
}