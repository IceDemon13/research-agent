using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    [DataContract]
    public sealed class CurrencyTypeRateSaveDto
    {
        public CurrencyTypeRateSaveDto(int id, decimal conversionRate)
        {
            Id = id;
            ConversionRate = conversionRate;
        }

        public CurrencyTypeRateSaveDto()
        {
        }

        [DataMember(Order = 1)]
        [JsonProperty("id")]
        public int Id { get; set; }

        [DataMember(Order = 2)]
        [JsonProperty("conversion_rate")]
        public decimal ConversionRate { get; set; }
    }
}