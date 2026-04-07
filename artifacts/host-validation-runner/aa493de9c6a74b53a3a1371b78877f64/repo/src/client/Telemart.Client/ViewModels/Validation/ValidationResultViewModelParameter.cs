using System.Collections.Generic;

namespace Telemart.Client.ViewModels.Validation
{
    public sealed class ValidationResultViewModelParameter
    {
        public ValidationResultViewModelParameter(string title, IEnumerable<ValidationResultItem> validationItems)
        {
            Title = title;
            ValidationItems = validationItems;
        }

        public string Title { get; }

        public IEnumerable<ValidationResultItem> ValidationItems { get; }
    }
}