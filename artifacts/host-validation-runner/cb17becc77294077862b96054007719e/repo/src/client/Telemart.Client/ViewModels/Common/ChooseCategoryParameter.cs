using System.Collections.Generic;

namespace Telemart.Client.ViewModels.Common
{
    public sealed class ChooseCategoryParameter
    {
        public ChooseCategoryParameter(bool multiSelect, IReadOnlyCollection<int> allowedCategoryIds, string allowedCategoriesErrorMessage = null)
        {
            AllowedCategoryIds = allowedCategoryIds;
            MultiSelect = multiSelect;
            AllowedCategoriesErrorMessage = allowedCategoriesErrorMessage;
        }

        public bool MultiSelect { get; }

        public IReadOnlyCollection<int> AllowedCategoryIds { get; }

        public string AllowedCategoriesErrorMessage { get; }
    }
}