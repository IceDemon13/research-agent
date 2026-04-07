using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public class CategoryMaskSaveDto
    {
        public CategoryMaskSaveDto(int id, string fmaskT, int languageId)
        {
            Id = id;
            FmaskT = fmaskT;
            LanguageId = languageId;
        }

        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("fmask_t")]
        public string FmaskT { get; set; }

        [JsonProperty("language_id")]
        public int LanguageId { get; set; }
    }
}