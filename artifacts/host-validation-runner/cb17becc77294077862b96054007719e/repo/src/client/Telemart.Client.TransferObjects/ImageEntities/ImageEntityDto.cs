using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.ImageEntities
{
    public sealed class ImageEntityDto
    {
        [JsonProperty("id_image")]
        public int Id { get; init; }

        [JsonProperty("id_entity")]
        public int EntityId { get; init; }

        [JsonProperty("entity_type")]
        public string EntityType { get; init; }

        [JsonProperty("title")]
        public string Title { get; init; }

        [JsonProperty("url")]
        public string Url { get; init; }

        [JsonProperty("ext")]
        public string Ext { get; init; }

        [JsonProperty("position")]
        public short Position { get; init; }

        [JsonProperty("cover")]
        public bool Cover { get; init; }
    }
}