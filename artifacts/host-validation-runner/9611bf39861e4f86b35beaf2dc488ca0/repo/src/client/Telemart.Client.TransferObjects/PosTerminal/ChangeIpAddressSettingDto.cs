using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.PosTerminal
{
    public sealed class ChangeIpAddressSettingDto
    {
        public ChangeIpAddressSettingDto(string ipAddress, string macAddress)
        {
            IpAddress = ipAddress;
            MacAddress = macAddress;
        }

        [JsonProperty("ip_address")]
        public string IpAddress { get; }

        [JsonProperty("mac_address")]
        public string MacAddress { get; }
    }
}