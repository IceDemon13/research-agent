using System.Collections.Specialized;
using System.Linq;
using System.Windows;
using DevExpress.Xpf.Bars;

namespace Telemart.Client.ViewComponents
{
    public sealed class BarLinkContainerItemEx : BarLinkContainerItem
    {
        public static readonly DependencyProperty HasEnabledItemsProperty = DependencyProperty.Register(
            "HasEnabledItems",
            typeof(bool),
            typeof(BarLinkContainerItemEx),
            new PropertyMetadata(true));

        public BarLinkContainerItemEx()
        {
            Items.CollectionChanged += ItemsCollectionChanged;
        }

        public bool HasEnabledItems
        {
            get => (bool)GetValue(HasEnabledItemsProperty);
            set => SetValue(HasEnabledItemsProperty, value);
        }

        private void ItemsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
            {
                foreach (BarItem barItem in e.NewItems)
                {
                    barItem.IsEnabledChanged += BarItemIsEnabledChanged;
                }
            }

            if (e.OldItems != null)
            {
                foreach (BarItem barItem in e.OldItems)
                {
                    barItem.IsEnabledChanged -= BarItemIsEnabledChanged;
                }
            }

            UpdateHasEnabledItems();
        }

        private void BarItemIsEnabledChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            UpdateHasEnabledItems();
        }

        private void UpdateHasEnabledItems()
        {
            HasEnabledItems = Items
                .OfType<FrameworkContentElement>()
                .Where(x => !(x is BarItemSeparator))
                .Any(x => x.IsEnabled);
        }
    }
}