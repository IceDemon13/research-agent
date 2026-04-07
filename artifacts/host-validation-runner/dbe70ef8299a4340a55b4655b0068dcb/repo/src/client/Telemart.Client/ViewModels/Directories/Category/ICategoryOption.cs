using Telemart.Client.Dictionaries;

namespace Telemart.Client.ViewModels.Directories.Category
{
    public interface ICategoryOption
    {
        string DisplayName { get; }

        object InitialValue { get; }

        object CurrentValue { get; }

        object ParentDisplayValue { get; }

        object InitialDisplayValue { get; }

        object CurrentDisplayValue { get; }

        CategoryOverrideOption OverrideOption { get; set; }

        CategoryOptionInheritanceMode InheritanceMode { get; }

        string OptionName { get; }

        bool IsCurrentValueChanged { get; }

        bool IsOverrideOptionEditable { get; }

        void ResetChanges();
    }
}