using System.Collections.Generic;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.Comment
{
    public class CommentFullDto : CommentSimpleDto
    {
        [JsonProperty("children")]
        public List<CommentSimpleDto> Children { get; set; }

        [JsonProperty("foto")]
        public bool Foto { get; set; }
    }
}