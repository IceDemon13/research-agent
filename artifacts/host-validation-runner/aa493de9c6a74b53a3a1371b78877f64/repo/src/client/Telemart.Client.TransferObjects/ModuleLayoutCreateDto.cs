using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public sealed class ModuleLayoutCreateDto
    {
        public ModuleLayoutCreateDto(string name, string layout, int moduleId, string parameters)
        {
            Name = name;
            Layout = layout;
            ModuleId = moduleId;
            Parameters = parameters;
        }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("layout")]
        public string Layout { get; set; }

        [JsonProperty("module_id")]
        public int ModuleId { get; set; }

        [JsonProperty("parameters")]
        public string Parameters { get; set; }
    }
}