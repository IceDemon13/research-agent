using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Telemart.Common.TreeStructure;

namespace Telemart.Client.TransferObjects
{
    public class ProductAdditionalServiceGroupDto : ITreeObject<ProductAdditionalServiceGroupDto>
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("name_ukr")]
        public string NameUkr { get; set; }

        [JsonProperty("name_en")]
        public string NameEn { get; set; }

        [JsonProperty("position")]
        public int Position { get; set; }

        [JsonProperty("groups")]
        public ICollection<ProductAdditionalServiceGroupDto> Groups { get; set; }

        [JsonProperty("additional_services")]
        public ICollection<ProductAdditionalServiceDto> AdditionalServices { get; set; }

        public ICollection<ProductAdditionalServiceGroupDto> GetChildObjects()
        {
            return Groups;
        }

        public int GetId()
        {
            return Id;
        }

        public void SetChildObjects(IEnumerable<ProductAdditionalServiceGroupDto> objects)
        {
            Groups = objects.ToArray();
        }
    }
}