using System.Collections.Generic;
using DevExpress.Mvvm.DataAnnotations;
using Telemart.Client.Common.MvvmEnhancements;
using Telemart.Client.Style.Editors;

namespace Telemart.Client.ViewModels.RobotProperties
{
    public class ValidationComboBoxValue : ComboBoxValue
    {
        public ValidationComboBoxValue(IReadOnlyCollection<ComboBoxItem> availValues, ComboBoxItem? selectedValue, bool active = false, bool isReadOnly = false)
            : base(availValues, selectedValue, isReadOnly)
        {
            Active = active;
        }

        public bool Active
        {
            get { return GetProperty(() => Active); }
            set { SetProperty(() => Active, value, () => RaisePropertiesChanged(nameof(SelectedValue))); }
        }

        public static void BuildMetadata(MetadataBuilder<ValidationComboBoxValue> builder)
        {
            builder.Property(x => x.SelectedValue)
                .MatchesInstanceRule((x, y) => y.IsReadOnly || !y.Active || !string.IsNullOrEmpty(x?.Ref), () => "Выберите из списка");
        }
    }
}