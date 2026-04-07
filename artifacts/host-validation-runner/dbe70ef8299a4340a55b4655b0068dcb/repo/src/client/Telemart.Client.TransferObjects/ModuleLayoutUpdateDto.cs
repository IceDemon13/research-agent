using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public sealed class ModuleLayoutUpdateDto
    {
        public ModuleLayoutUpdateDto(int id, string layout, string parameters)
        {
            Id = id;
            Layout = layout;
            Parameters = parameters;
        }

        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("layout")]
        public string Layout { get; set; }

        [JsonProperty("parameters")]
        public string Parameters { get; set; }
    }
}