using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public sealed class AssemblyTestPositionDto
    {
        public AssemblyTestPositionDto(int id, int position)
        {
            Id = id;
            Position = position;
        }

        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("position")]
        public int Position { get; set; }
    }
}