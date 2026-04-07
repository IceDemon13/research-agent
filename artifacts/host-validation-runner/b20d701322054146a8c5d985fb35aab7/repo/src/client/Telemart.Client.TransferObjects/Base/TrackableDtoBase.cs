using System;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.Base
{
    public abstract class TrackableDtoBase<TIdentity>
    {
        [JsonProperty("id")]
        public TIdentity Id { get; init; }

        [JsonProperty("modified_on")]
        public DateTime ModifiedOn { get; init; }
    }
}