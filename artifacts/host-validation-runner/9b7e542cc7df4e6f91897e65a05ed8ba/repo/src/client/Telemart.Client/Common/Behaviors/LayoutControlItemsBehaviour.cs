using System.Collections;
using System.Collections.Specialized;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using DevExpress.Mvvm.UI.Interactivity;
using DevExpress.Xpf.LayoutControl;

namespace Telemart.Client.Common.Behaviors
{
    internal class LayoutControlItemsBehaviour : Behavior<LayoutControl>
    {
        public static readonly DependencyProperty ItemsSourceProperty = DependencyProperty.Register(
            nameof(ItemsSource),
            typeof(object),
            typeof(LayoutControlItemsBehaviour),
            new UIPropertyMetadata(null, (d, e) => { ((LayoutControlItemsBehaviour)d).OnItemsSourceChanged(e.OldValue, e.NewValue); }));

        public static readonly DependencyProperty ItemTemplateProperty = DependencyProperty.Register(
            nameof(ItemTemplate),
            typeof(DataTemplate),
            typeof(LayoutControlItemsBehaviour),
            new UIPropertyMetadata(null));

        public static readonly DependencyProperty ItemTemplateSelectorProperty = DependencyProperty.Register(
            nameof(ItemTemplateSelector),
            typeof(DataTemplateSelector),
            typeof(LayoutControlItemsBehaviour),
            new UIPropertyMetadata(null));

        public static readonly DependencyProperty RootGroupNameProperty = DependencyProperty.Register(
           nameof(RootGroupName),
           typeof(string),
           typeof(LayoutControlItemsBehaviour),
           new UIPropertyMetadata(null));

        public object ItemsSource
        {
            get { return GetValue(ItemsSourceProperty); }
            set { SetValue(ItemsSourceProperty, value); }
        }

        public DataTemplate ItemTemplate
        {
            get { return (DataTemplate)GetValue(ItemTemplateProperty); }
            set { SetValue(ItemTemplateProperty, value); }
        }

        public DataTemplateSelector ItemTemplateSelector
        {
            get { return (DataTemplateSelector)GetValue(ItemTemplateSelectorProperty); }
            set { SetValue(ItemTemplateSelectorProperty, value); }
        }

        public string RootGroupName
        {
            get { return (string)GetValue(RootGroupNameProperty); }
            set { SetValue(RootGroupNameProperty, value); }
        }

        protected virtual void AddItem(object current)
        {
            LayoutControlItem item = current as LayoutControlItem;

            if (item == null)
            {
                return;
            }

            Binding itemTemplateBinding = new Binding(nameof(ItemTemplate))
            {
                Mode = BindingMode.TwoWay,
                Source = this
            };

            Binding itemTemplateSelectorBinding = new Binding(nameof(ItemTemplateSelector))
            {
                Source = this,
                Mode = BindingMode.OneWay
            };

            ContentControl layoutItemContent = new ContentControl
            {
                Content = item
            };

            layoutItemContent.SetBinding(ContentControl.ContentTemplateProperty, itemTemplateBinding);
            layoutItemContent.SetBinding(ContentControl.ContentTemplateSelectorProperty, itemTemplateSelectorBinding);

            LayoutItem layoutItem = new LayoutItem
            {
                Label = item.Label,
                Content = layoutItemContent
            };

            LayoutGroup layoutGroup = AssociatedObject.Children.OfType<LayoutGroup>().First(y => y.Name == RootGroupName);

            layoutGroup.Children.Add(layoutItem);
        }

        protected virtual void ArrangeChildren()
        {
            IEnumerable items = ItemsSource as IEnumerable;

            if (items != null)
            {
                foreach (object item in items)
                {
                    AddItem(item);
                }
            }
        }

        protected virtual void OnItemsSourceChanged(object oldValue, object newValue)
        {
            INotifyCollectionChanged collection = newValue as INotifyCollectionChanged;

            if (collection != null)
            {
                collection.CollectionChanged += OnItemsSourceCollectionChanged;
            }

            ArrangeChildren();
        }

        protected virtual void OnItemsSourceCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Reset)
            {
                AssociatedObject.Children.Clear();
            }

            if (e.NewItems != null)
            {
                foreach (object item in e.NewItems)
                {
                    AddItem(item);
                }
            }

            if (e.OldItems != null)
            {
                foreach (object item in e.OldItems)
                {
                    RemoveItem(item);
                }
            }
        }

        protected virtual void RemoveItem(object current)
        {
            LayoutGroup layoutGroup = AssociatedObject.Children
                .OfType<LayoutGroup>()
                .First(y => y.Name == RootGroupName);

            LayoutItem layoutItem = layoutGroup.Children
                .OfType<LayoutItem>()
                .FirstOrDefault(el => el.DataContext.Equals(current));

            layoutGroup.Children.Remove(layoutItem);
        }
    }
}