using System;
using System.Text;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public class DeliveryDataDto : IEquatable<DeliveryDataDto>
    {
        [JsonProperty("city_id")]
        public string CityId { get; set; }

        [JsonProperty("place_id")]
        public string PlaceId { get; set; }

        [JsonProperty("street")]
        public string Street { get; set; }

        [JsonProperty("house")]
        public string House { get; set; }

        [JsonProperty("flat")]
        public string Flat { get; set; }

        [JsonProperty("extra")]
        public string Extra { get; set; }

        [JsonProperty("max_allowed_weight")]
        public double? MaxAllowedWeight { get; set; }

        [JsonProperty("address")]
        public string Address { get; set; }

        [JsonProperty("address_ukr")]
        public string AddressUkr { get; set; }

        [JsonProperty("address_en")]
        public string AddressEn { get; set; }

        [JsonProperty("index")]
        public string Index { get; set; }

        public bool Equals(DeliveryDataDto other)
        {
            if (ReferenceEquals(null, other))
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return string.Equals(CityId, other.CityId, StringComparison.OrdinalIgnoreCase)
                   && string.Equals(PlaceId, other.PlaceId, StringComparison.OrdinalIgnoreCase)
                   && string.Equals(Street, other.Street, StringComparison.OrdinalIgnoreCase)
                   && string.Equals(House, other.House, StringComparison.OrdinalIgnoreCase)
                   && string.Equals(Flat, other.Flat, StringComparison.OrdinalIgnoreCase)
                   && string.Equals(Extra, other.Extra, StringComparison.OrdinalIgnoreCase)
                   && string.Equals(Address, other.Address, StringComparison.OrdinalIgnoreCase)
                   && string.Equals(AddressEn, other.AddressEn, StringComparison.OrdinalIgnoreCase)
                   && string.Equals(AddressUkr, other.AddressUkr, StringComparison.OrdinalIgnoreCase)
                   && string.Equals(Index, other.AddressUkr, StringComparison.OrdinalIgnoreCase)
                   && MaxAllowedWeight.Equals(other.MaxAllowedWeight);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }

            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            return obj.GetType() == GetType() && Equals((DeliveryDataDto)obj);
        }

        public override int GetHashCode()
        {
            HashCode hashCode = default;
            hashCode.Add(CityId);
            hashCode.Add(PlaceId);
            hashCode.Add(Street);
            hashCode.Add(House);
            hashCode.Add(Flat);
            hashCode.Add(Extra);
            hashCode.Add(MaxAllowedWeight);
            hashCode.Add(Address);
            hashCode.Add(AddressUkr);
            hashCode.Add(AddressEn);
            hashCode.Add(Index);
            return hashCode.ToHashCode();
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append(Address);

            if (!string.IsNullOrWhiteSpace(House))
            {
                sb.Append(", буд. ");
                sb.Append(House);
            }

            if (!string.IsNullOrWhiteSpace(Flat))
            {
                sb.Append(", кв. ");
                sb.Append(Flat);
            }

            if (!string.IsNullOrWhiteSpace(Extra))
            {
                sb.Append(", ");
                sb.Append(Extra);
            }

            return sb.ToString();
        }
    }
}