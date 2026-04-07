using System.Text.Json.Serialization;

namespace Telemart.Client.FiscalRegistrar.Abstraction.TransferObjects
{
    public class PrintResponse
    {
        [JsonPropertyName("body")]
        public string Body { get; set; }
    }
}