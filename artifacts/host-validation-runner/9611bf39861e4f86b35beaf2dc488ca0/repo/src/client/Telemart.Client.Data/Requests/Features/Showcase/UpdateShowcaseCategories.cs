using System.Collections.Generic;
using Newtonsoft.Json;
using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Showcase
{
    public class UpdateShowcaseCategories : CallActionWithBodyRequestResultBase<List<ShowcaseCategoryDto>, UpdateShowcaseCategories.UpdateShowcaseCategoriesDto>
    {
        public UpdateShowcaseCategories(IReadOnlyCollection<ShowcaseCategorySaveDto> showcaseCategories)
            : base(new UpdateShowcaseCategoriesDto(showcaseCategories), ApiResources.Showcases, "categories")
        {
        }

        public class UpdateShowcaseCategoriesDto
        {
            public UpdateShowcaseCategoriesDto(IReadOnlyCollection<ShowcaseCategorySaveDto> showcaseCategories)
            {
                ShowcaseCategories = showcaseCategories;
            }

            [JsonProperty("showcase_categories")]
            public IReadOnlyCollection<ShowcaseCategorySaveDto> ShowcaseCategories { get; }
        }
    }
}