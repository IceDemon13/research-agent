using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.RobotProperty
{
    public sealed class RobotCategoryPropertyValueSaveDto
    {
        public RobotCategoryPropertyValueSaveDto(int id, int propertyId, string value, bool active, int saveTypeId, bool isChanged)
        {
            Id = id;
            PropertyId = propertyId;
            Value = value;
            Active = active;
            ValueSaveTypeId = saveTypeId;
            IsChanged = isChanged;
        }

        [JsonProperty("id")]
        public int Id { get; init; }

        [JsonProperty("property_id")]
        public int PropertyId { get; init; }

        [JsonProperty("value")]
        public string Value { get; init; }

        [JsonProperty("active")]
        public bool Active { get; init; }

        [JsonProperty("value_save_type_id")]
        public int ValueSaveTypeId { get; init; }

        [JsonProperty("is_changed")]
        public bool IsChanged { get; init; }
    }
}