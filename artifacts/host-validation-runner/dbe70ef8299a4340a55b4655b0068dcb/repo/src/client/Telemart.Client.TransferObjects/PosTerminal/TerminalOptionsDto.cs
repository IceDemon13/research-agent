using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.PosTerminal
{
    public sealed record TerminalOptionsDto
    {
        [JsonProperty("check_mac_address")]
        public bool CheckMacAddress { get; init; }
    }
}