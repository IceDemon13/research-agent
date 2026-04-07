using System.Collections.Generic;
using DevExpress.Mvvm.DataAnnotations;
using Telemart.Client.Common.MvvmEnhancements;
using Telemart.Client.Style.Editors;

namespace Telemart.Client.ViewModels.RobotProperties
{
    public class ValidationMultiComboBoxValue : MultiComboBoxValue
    {
        public ValidationMultiComboBoxValue(IReadOnlyCollection<ComboBoxItem> availValues, IReadOnlyCollection<ComboBoxItem> selectedValues, bool active = false, bool isReadOnly = false)
            : base(availValues, selectedValues, isReadOnly)
        {
            Active = active;
        }

        public bool Active
        {
            get { return GetProperty(() => Active); }
            set { SetProperty(() => Active, value, () => RaisePropertiesChanged(nameof(SelectedValues))); }
        }

        public static void BuildMetadata(MetadataBuilder<ValidationMultiComboBoxValue> builder)
        {
            builder.Property(x => x.SelectedValues)
                .MatchesInstanceRule((x, y) => y.IsReadOnly || !y.Active || x?.Count > 0, () => "Выберите из списка");
        }
    }
}