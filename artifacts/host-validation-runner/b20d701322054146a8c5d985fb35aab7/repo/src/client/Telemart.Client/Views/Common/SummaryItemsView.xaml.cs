using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using Telemart.Client.Common;

namespace Telemart.Client.Views.Common
{
    /// <summary>
    /// Interaction logic for SummaryItemsView.xaml
    /// </summary>
    public partial class SummaryItemsView : UserControl
    {
        public static readonly DependencyProperty LabelColumnWidthProperty = DependencyProperty.Register(
            "LabelColumnWidth",
            typeof(string),
            typeof(SummaryItemsView),
            new PropertyMetadata("*"));

        public static readonly DependencyProperty ContentColumnWidthProperty = DependencyProperty.Register(
            "ContentColumnWidth",
            typeof(string),
            typeof(SummaryItemsView),
            new PropertyMetadata("2*"));

        public static readonly DependencyProperty SummaryItemsProperty = DependencyProperty.Register(
            "SummaryItems",
            typeof(IEnumerable<SummaryViewItem>),
            typeof(SummaryItemsView),
            new PropertyMetadata(null));

        public SummaryItemsView()
        {
            InitializeComponent();

            BorderThickness = new Thickness(1);
        }

        public string LabelColumnWidth
        {
            get => (string)GetValue(LabelColumnWidthProperty);
            set => SetValue(LabelColumnWidthProperty, value);
        }

        public string ContentColumnWidth
        {
            get => (string)GetValue(ContentColumnWidthProperty);
            set => SetValue(ContentColumnWidthProperty, value);
        }

        public IEnumerable<SummaryViewItem> SummaryItems
        {
            get => (IEnumerable<SummaryViewItem>)GetValue(SummaryItemsProperty);
            set => SetValue(SummaryItemsProperty, value);
        }
    }
}
