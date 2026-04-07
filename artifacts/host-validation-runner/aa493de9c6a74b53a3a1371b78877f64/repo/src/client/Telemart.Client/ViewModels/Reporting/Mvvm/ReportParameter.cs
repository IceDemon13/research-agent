using System.Collections.ObjectModel;
using Telemart.Client.Common.Behaviors;
using Telemart.Client.Common.MvvmEnhancements;

namespace Telemart.Client.ViewModels.Reporting.Mvvm
{
    internal class ReportParameter : LayoutControlItem
    {
        public ReadOnlyObservableCollection<ComboBoxItem> ComboBoxItems
        {
            get { return GetProperty(() => ComboBoxItems); }
            set { SetProperty(() => ComboBoxItems, value); }
        }

        public ReportParameterEditorType EditorType
        {
            get { return GetProperty(() => EditorType); }
            set { SetProperty(() => EditorType, value); }
        }

        public object Value
        {
            get { return GetProperty(() => Value); }
            set { SetProperty(() => Value, value); }
        }
    }
}