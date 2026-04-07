using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.AutoSource
{
    public sealed class SourceSettingValueDto
    {
        public SourceSettingValueDto(int id, string sourceValue)
        {
            Id = id;
            SourceValue = sourceValue;
        }

        [JsonProperty("id")]
        public int Id { get; }

        [JsonProperty("value")]
        public string SourceValue { get; }
    }
}