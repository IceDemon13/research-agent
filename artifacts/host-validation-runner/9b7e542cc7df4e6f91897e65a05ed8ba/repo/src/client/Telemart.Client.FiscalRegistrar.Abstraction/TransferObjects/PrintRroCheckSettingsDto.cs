using System.Text.Json.Serialization;

namespace Telemart.Client.FiscalRegistrar.Abstraction.TransferObjects
{
    public sealed class PrintRroCheckSettingsDto
    {
        [JsonPropertyName("email")]
        public bool Email { get; set; }

        [JsonPropertyName("phone")]
        public bool Phone { get; set; }

        [JsonPropertyName("printer")]
        public bool Printer { get; set; }
    }
}