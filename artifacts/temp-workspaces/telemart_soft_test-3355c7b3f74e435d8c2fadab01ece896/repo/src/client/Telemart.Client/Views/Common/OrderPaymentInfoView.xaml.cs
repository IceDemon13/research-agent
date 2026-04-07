using System.Collections.ObjectModel;
using System.Windows;
using Telemart.Client.ViewModels.Store.Order.OrderPaymentInfo;

namespace Telemart.Client.Views.Common
{
    /// <summary>
    /// Interaction logic for OrderPaymentInfoView.xaml
    /// </summary>
    public partial class OrderPaymentInfoView
    {
        public static readonly DependencyProperty ItemsProperty = DependencyProperty.Register(
            nameof(Items),
            typeof(ObservableCollection<OrderPaymentInfoItem>),
            typeof(OrderPaymentInfoView),
            new PropertyMetadata(default(ObservableCollection<OrderPaymentInfoItem>)));

        public static readonly DependencyProperty IsUsdVisibleProperty = DependencyProperty.Register(
            nameof(IsUsdVisible),
            typeof(bool),
            typeof(OrderPaymentInfoView),
            new PropertyMetadata(default(bool)));

        public static readonly DependencyProperty ShowBorderProperty = DependencyProperty.Register(
            nameof(ShowBorder),
            typeof(bool),
            typeof(OrderPaymentInfoView),
            new PropertyMetadata(true));

        public OrderPaymentInfoView()
        {
            InitializeComponent();
        }

        public ObservableCollection<OrderPaymentInfoItem> Items
        {
            get => (ObservableCollection<OrderPaymentInfoItem>)GetValue(ItemsProperty);
            set => SetValue(ItemsProperty, value);
        }

        public bool IsUsdVisible
        {
            get => (bool)GetValue(IsUsdVisibleProperty);
            set => SetValue(IsUsdVisibleProperty, value);
        }

        public bool ShowBorder
        {
            get => (bool)GetValue(ShowBorderProperty);
            set => SetValue(ShowBorderProperty, value);
        }
    }
}
