using System.Collections.Generic;
using System.Collections.ObjectModel;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using Telemart.Client.Common.MvvmEnhancements;

namespace Telemart.Client.Controls.Accordion
{
    public class CheckedListAccordionItem : BindableBase, IAccordionItem
    {
        public CheckedListAccordionItem(IEnumerable<ComboBoxItem> items, string title, bool isExpanded, string name = null)
        {
            Items = items.ToReadOnlyObservableCollection();
            Title = title;
            IsExpanded = isExpanded;
            Name = name;
        }

        public ReadOnlyObservableCollection<ComboBoxItem> Items { get; }

        public ObservableCollection<int> SelectedItems
        {
            get { return GetProperty(() => SelectedItems); }
            set { SetProperty(() => SelectedItems, value); }
        }

        public string Title { get; }

        public string Name { get; }

        public bool IsExpanded { get; }

        public void Cancel()
        {
            SelectedItems = null;
        }
    }
}