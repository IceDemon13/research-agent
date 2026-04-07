using System.Text.Json.Serialization;

namespace Telemart.Client.FiscalRegistrar.Abstraction.TransferObjects
{
    public class CashCollectionResponse
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }
    }
}