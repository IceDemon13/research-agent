using System.Collections.Generic;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.AutoSource
{
    public sealed class AutoSourceSettingsSaveDto
    {
        public AutoSourceSettingsSaveDto(IReadOnlyCollection<SourceSettingValueDto> values)
        {
            AutoSourceSettings = values;
        }

        [JsonProperty("auto_source_settings")]
        public IReadOnlyCollection<SourceSettingValueDto> AutoSourceSettings { get; }
    }
}