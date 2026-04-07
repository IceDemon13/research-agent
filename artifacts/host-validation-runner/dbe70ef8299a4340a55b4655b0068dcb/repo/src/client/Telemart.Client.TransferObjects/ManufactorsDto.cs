using System.Collections.Generic;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public sealed record ManufactorsDto
    {
        [JsonProperty("manufactors")]
        public IReadOnlyCollection<ManufactorDto> Manufactors { get; init; }
    }
}