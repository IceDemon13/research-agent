using Newtonsoft.Json;
using Telemart.Client.TransferObjects.CategoryOptions;

namespace Telemart.Client.TransferObjects
{
    public class CategoryOptionSaveDto
    {
        public CategoryOptionSaveDto(
            string propertyName,
            object propertyValueNew,
            CategoryOverrideOptions overrideOption = CategoryOverrideOptions.OverrideInDescendantsWithSameValue)
        {
            PropertyName = propertyName;
            PropertyValueNew = propertyValueNew;
            OverrideOption = overrideOption;
        }

        [JsonProperty("name")]
        public string PropertyName { get; set; }

        [JsonProperty("value")]
        public object PropertyValueNew { get; set; }

        [JsonProperty("override_option")]
        public CategoryOverrideOptions OverrideOption { get; set; }
    }
}