using System;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public class DeliveryTypeDto : IEquatable<DeliveryTypeDto>
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("carry_id")]
        public int CarryId { get; set; }

        public bool Equals(DeliveryTypeDto other)
        {
            return Id == other?.Id;
        }

        public override bool Equals(object obj)
        {
            if (obj is DeliveryTypeDto other)
            {
                return Id == other.Id;
            }

            return false;
        }

        public override int GetHashCode()
        {
            return Id;
        }
    }
}