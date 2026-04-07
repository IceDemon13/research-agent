using System.Collections.Generic;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public class ProductUploadImagesDto
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("images")]
        public IReadOnlyCollection<ImageDto> Images { get; set; }
    }
}