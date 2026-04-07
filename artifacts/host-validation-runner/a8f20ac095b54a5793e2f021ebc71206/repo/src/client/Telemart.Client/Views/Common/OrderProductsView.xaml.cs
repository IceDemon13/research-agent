using System.Collections.ObjectModel;
using System.Windows;
using Telemart.Client.ViewModels.Store;

namespace Telemart.Client.Views.Common
{
    /// <summary>
    /// Interaction logic for OrderProductsView.xaml
    /// </summary>
    public partial class OrderProductsView
    {
        public static readonly DependencyProperty OrderProductsProperty = DependencyProperty.Register(
            nameof(OrderProducts),
            typeof(ObservableCollection<OrderProductViewItem>),
            typeof(OrderProductsView),
            new PropertyMetadata(default(ObservableCollection<OrderProductViewItem>)));

        public static readonly DependencyProperty SelectedOrderProductsProperty = DependencyProperty.Register(
            nameof(SelectedOrderProducts),
            typeof(ObservableCollection<OrderProductViewItem>),
            typeof(OrderProductsView),
            new PropertyMetadata(default(ObservableCollection<OrderProductViewItem>)));

        public static readonly DependencyProperty ShowBorderProperty = DependencyProperty.Register(
            nameof(ShowBorder),
            typeof(bool),
            typeof(OrderProductsView),
            new PropertyMetadata(true));

        public static readonly DependencyProperty ShowCheckBoxSelectorColumnProperty = DependencyProperty.Register(
            nameof(ShowCheckBoxSelectorColumn),
            typeof(bool),
            typeof(OrderProductsView),
            new PropertyMetadata(false));

        public OrderProductsView()
        {
            InitializeComponent();
        }

        public ObservableCollection<OrderProductViewItem> OrderProducts
        {
            get => (ObservableCollection<OrderProductViewItem>)GetValue(OrderProductsProperty);
            set => SetValue(OrderProductsProperty, value);
        }

        public ObservableCollection<OrderProductViewItem> SelectedOrderProducts
        {
            get => (ObservableCollection<OrderProductViewItem>)GetValue(SelectedOrderProductsProperty);
            set => SetValue(SelectedOrderProductsProperty, value);
        }

        public bool ShowBorder
        {
            get => (bool)GetValue(ShowBorderProperty);
            set => SetValue(ShowBorderProperty, value);
        }

        public bool ShowCheckBoxSelectorColumn
        {
            get => (bool)GetValue(ShowCheckBoxSelectorColumnProperty);
            set => SetValue(ShowCheckBoxSelectorColumnProperty, value);
        }
    }
}
