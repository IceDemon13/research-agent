using Newtonsoft.Json;
using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.AdditionalService
{
    public sealed class UpdateAdditionalServiceGroup : UpdateEntityResultRequestBase<AdditionalServiceGroupDto, UpdateAdditionalServiceGroup.AdditionalServiceGroupUpdateDto>
    {
        public UpdateAdditionalServiceGroup(int groupId, string name, string nameUa, string nameEn, string description, string descriptionUa, string descriptionEn, bool multiSelect)
            : base(new AdditionalServiceGroupUpdateDto(groupId, name, nameUa, nameEn, description, descriptionUa, descriptionEn, multiSelect), ApiResources.AdditionalServicesGroups, groupId)
        {
        }

        public class AdditionalServiceGroupUpdateDto
        {
            public AdditionalServiceGroupUpdateDto(int groupId, string name, string nameUa, string nameEn, string description, string descriptionUa, string descriptionEn, bool multiSelect)
            {
                GroupId = groupId;
                Name = name;
                NameUa = nameUa;
                NameEn = nameEn;
                Description = description;
                DescriptionUa = descriptionUa;
                DescriptionEn = descriptionEn;
                MultiSelect = multiSelect;
            }

            [JsonProperty("id_group")]
            public int GroupId { get; set; }

            [JsonProperty("name")]
            public string Name { get; set; }

            [JsonProperty("name_ua")]
            public string NameUa { get; set; }

            [JsonProperty("name_en")]
            public string NameEn { get; set; }

            [JsonProperty("description")]
            public string Description { get; set; }

            [JsonProperty("description_ua")]
            public string DescriptionUa { get; set; }

            [JsonProperty("description_en")]
            public string DescriptionEn { get; set; }

            [JsonProperty("multi_select")]
            public bool MultiSelect { get; set; }
        }
    }
}