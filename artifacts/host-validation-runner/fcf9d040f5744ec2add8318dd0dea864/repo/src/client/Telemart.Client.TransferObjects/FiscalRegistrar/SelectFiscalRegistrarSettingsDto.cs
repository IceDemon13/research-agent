using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.FiscalRegistrar
{
    public sealed class SelectFiscalRegistrarSettingsDto
    {
        public SelectFiscalRegistrarSettingsDto(
            int id,
            string uniqueDeviceId)
        {
            Id = id;
            UniqueDeviceId = uniqueDeviceId;
        }

        [JsonProperty("id")]
        public int Id { get; }

        [JsonProperty("id_unique_device")]
        public string UniqueDeviceId { get; }
    }
}