using Newtonsoft.Json;
using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;
using static Telemart.Client.Data.Requests.Features.AssemblyTest.UpdateAssemblyTestGroup;

namespace Telemart.Client.Data.Requests.Features.AssemblyTest
{
    public class UpdateAssemblyTestGroup : UpdateEntityResultRequestBase<AssemblyTestGroupDto, AssemblyTestGroupUpdateDto>
    {
        public UpdateAssemblyTestGroup(int groupId, string name, string nameUa, string nameEn)
            : base(new AssemblyTestGroupUpdateDto(groupId, name, nameUa, nameEn), $"{ApiResources.AssemblyTests}/groups", groupId)
        {
        }

        public class AssemblyTestGroupUpdateDto
        {
            public AssemblyTestGroupUpdateDto(int groupId, string name, string nameUa, string nameEn)
            {
                GroupId = groupId;
                Name = name;
                NameUa = nameUa;
                NameEn = nameEn;
            }

            [JsonProperty("id_group")]
            public int GroupId { get; set; }

            [JsonProperty("name")]
            public string Name { get; set; }

            [JsonProperty("name_ua")]
            public string NameUa { get; set; }

            [JsonProperty("name_en")]
            public string NameEn { get; set; }
        }
    }
}