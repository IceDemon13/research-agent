using System.Collections.Generic;
using Newtonsoft.Json;
using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects.TradeInSegment;

namespace Telemart.Client.Data.Requests.Features.TradeInSegment
{
    public sealed class UpdateTradeInSegmentCategorySettings : CallActionWithBodyRequestResultBase<List<TradeInSegmentCategorySettingsDto>, UpdateTradeInSegmentCategorySettings.TradeInSegmentCategorySettingsUpdateRequestDto>
    {
        public UpdateTradeInSegmentCategorySettings(ICollection<TradeInSegmentCategorySettingsUpdateDto> updateDtos)
            : base(new TradeInSegmentCategorySettingsUpdateRequestDto(updateDtos), $"{ApiResources.TradeInSegments}", "category_settings")
        {
        }

        public class TradeInSegmentCategorySettingsUpdateRequestDto
        {
            public TradeInSegmentCategorySettingsUpdateRequestDto(ICollection<TradeInSegmentCategorySettingsUpdateDto> categorySettings)
            {
                CategorySettings = categorySettings;
            }

            [JsonProperty("category_settings")]
            public ICollection<TradeInSegmentCategorySettingsUpdateDto> CategorySettings { get; set; }
        }
    }
}