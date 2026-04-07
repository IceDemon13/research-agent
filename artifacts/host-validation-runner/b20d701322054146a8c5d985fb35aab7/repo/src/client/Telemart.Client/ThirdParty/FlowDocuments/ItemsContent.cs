using System;
using System.Collections;
using System.Windows;
using System.Windows.Documents;

namespace Telemart.Client.ThirdParty.FlowDocuments
{
    public sealed class ItemsContent : Section
    {
        private static readonly DependencyProperty ItemsSourceProperty = DependencyProperty.Register(
            nameof(ItemsSource),
            typeof(IEnumerable),
            typeof(ItemsContent),
            new PropertyMetadata(OnItemsSourceChanged));

        private static readonly DependencyProperty ItemTemplateProperty = DependencyProperty.Register(
            nameof(ItemTemplate),
            typeof(DataTemplate),
            typeof(ItemsContent),
            new PropertyMetadata(OnItemTemplateChanged));

        private static readonly DependencyProperty ItemsPanelProperty = DependencyProperty.Register(
            nameof(ItemsPanel),
            typeof(DataTemplate),
            typeof(ItemsContent),
            new PropertyMetadata(OnItemsPanelChanged));

        private FrameworkContentElement panel;

        public ItemsContent()
        {
            Helpers.FixupDataContext(this);
            Loaded += ItemsContentLoaded;
            SourceUpdated += ItemsContentSourceUpdated;
        }

        public IEnumerable ItemsSource
        {
            get => (IEnumerable)GetValue(ItemsSourceProperty);
            set => SetValue(ItemsSourceProperty, value);
        }

        public DataTemplate ItemTemplate
        {
            get => (DataTemplate)GetValue(ItemTemplateProperty);
            set => SetValue(ItemTemplateProperty, value);
        }

        public DataTemplate ItemsPanel
        {
            get => (DataTemplate)GetValue(ItemsPanelProperty);
            set => SetValue(ItemsPanelProperty, value);
        }

        private static void OnItemsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ItemsContent itemsContent = (ItemsContent)d;
            itemsContent.OnItemsSourceChanged((IEnumerable)e.NewValue);
        }

        private static void OnItemTemplateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ItemsContent itemsContent = (ItemsContent)d;
            itemsContent.OnItemTemplateChanged((DataTemplate)e.NewValue);
        }

        private static void OnItemsPanelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ItemsContent itemsContent = (ItemsContent)d;
            itemsContent.OnItemsPanelChanged((DataTemplate)e.NewValue);
        }

        private void ItemsContentLoaded(object sender, RoutedEventArgs e)
        {
            GenerateContent(ItemsPanel, ItemTemplate, ItemsSource);
        }

        private void ItemsContentSourceUpdated(object sender, RoutedEventArgs e)
        {
            GenerateContent(ItemsPanel, ItemTemplate, ItemsSource);
        }

        private void GenerateContent(DataTemplate itemsPanel, DataTemplate itemTemplate, IEnumerable itemsSource)
        {
            if (itemsPanel == null || itemTemplate == null || itemsSource == null)
            {
                return;
            }

            if (panel == null)
            {
                panel = GetPanel(itemsPanel);
            }

            switch (panel)
            {
                case Section section:

                    section.Blocks.Clear();

                    foreach (object data in itemsSource)
                    {
                        FrameworkContentElement element = Helpers.LoadDataTemplate(itemTemplate);
                        element.DataContext = data;
                        Helpers.UnFixupDataContext(element);

                        section.Blocks.Add(Helpers.ConvertToBlock(data, element));
                    }

                    break;
                case TableRowGroup tableRowGroup:

                    tableRowGroup.Rows.Clear();

                    foreach (object data in itemsSource)
                    {
                        FrameworkContentElement element = Helpers.LoadDataTemplate(itemTemplate);
                        element.DataContext = data;
                        Helpers.UnFixupDataContext(element);

                        tableRowGroup.Rows.Add((TableRow)element);
                    }

                    break;
                default:
                    throw new InvalidOperationException($"Don't know how to add an element instance to an instance of {panel.GetType()}");
            }
        }

        private FrameworkContentElement GetPanel(DataTemplate itemsPanel)
        {
            FrameworkContentElement itemsPanelTemplate = Helpers.LoadDataTemplate(itemsPanel);

            Block block = itemsPanelTemplate as Block;

            if (block == null)
            {
                throw new InvalidOperationException("ItemsPanel must be a block element");
            }

            Blocks.Add(block);

            FrameworkContentElement result = Attached.GetItemsHost(itemsPanelTemplate);

            if (result == null)
            {
                throw new InvalidOperationException("ItemsHost not found. Did you forget to specify Attached.IsItemsHost?");
            }

            return result;
        }

        private void OnItemsSourceChanged(IEnumerable newValue)
        {
            if (IsLoaded)
            {
                GenerateContent(ItemsPanel, ItemTemplate, newValue);
            }
        }

        private void OnItemTemplateChanged(DataTemplate newValue)
        {
            if (IsLoaded)
            {
                GenerateContent(ItemsPanel, newValue, ItemsSource);
            }
        }

        private void OnItemsPanelChanged(DataTemplate newValue)
        {
            if (IsLoaded)
            {
                GenerateContent(newValue, ItemTemplate, ItemsSource);
            }
        }
    }
}
