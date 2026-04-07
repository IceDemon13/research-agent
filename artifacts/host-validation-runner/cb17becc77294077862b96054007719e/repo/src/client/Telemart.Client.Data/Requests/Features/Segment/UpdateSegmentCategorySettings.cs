using System.Collections.Generic;
using Newtonsoft.Json;
using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects.Segment;

namespace Telemart.Client.Data.Requests.Features.Segment
{
    public sealed class UpdateSegmentCategorySettings : CallActionWithBodyRequestResultBase<List<SegmentCategorySettingsDto>, UpdateSegmentCategorySettings.SegmentCategorySettingsUpdateRequestDto>
    {
        public UpdateSegmentCategorySettings(ICollection<SegmentCategorySettingsUpdateDto> updateDtos)
            : base(new SegmentCategorySettingsUpdateRequestDto(updateDtos), $"{ApiResources.Segments}", "category_settings")
        {
        }

        public class SegmentCategorySettingsUpdateRequestDto
        {
            public SegmentCategorySettingsUpdateRequestDto(ICollection<SegmentCategorySettingsUpdateDto> categorySettings)
            {
                CategorySettings = categorySettings;
            }

            [JsonProperty("category_settings")]
            public ICollection<SegmentCategorySettingsUpdateDto> CategorySettings { get; set; }
        }
    }
}