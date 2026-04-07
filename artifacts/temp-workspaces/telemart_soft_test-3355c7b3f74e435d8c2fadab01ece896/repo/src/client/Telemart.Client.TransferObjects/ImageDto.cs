using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public class ImageDto
    {
        public ImageDto(string fileName, byte[] content)
        {
            FileName = fileName;
            Content = content;
        }

        public ImageDto()
        {
        }

        [JsonProperty("file_name")]
        public string FileName { get; set; }

        [JsonProperty("file_content")]
        public byte[] Content { get; set; }
    }
}