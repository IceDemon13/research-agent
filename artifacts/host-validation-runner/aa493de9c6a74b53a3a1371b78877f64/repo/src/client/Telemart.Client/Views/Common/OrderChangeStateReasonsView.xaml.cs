using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using DevExpress.Xpf.Grid;
using Telemart.Client.ViewModels.Store;

namespace Telemart.Client.Views.Common
{
    /// <summary>
    ///     Interaction logic for OrderChangeStateReasonsView.xaml
    /// </summary>
    public partial class OrderChangeStateReasonsView
    {
        public static readonly DependencyProperty CurrentReasonProperty = DependencyProperty.Register(
            nameof(CurrentReason),
            typeof(OrderStateChangeReasonViewItem),
            typeof(OrderChangeStateReasonsView),
            new PropertyMetadata(default(OrderStateChangeReasonViewItem)));

        public static readonly DependencyProperty ReasonsProperty = DependencyProperty.Register(
            nameof(Reasons),
            typeof(ObservableCollection<OrderStateChangeReasonViewItem>),
            typeof(OrderChangeStateReasonsView),
            new PropertyMetadata(default(ObservableCollection<OrderStateChangeReasonViewItem>)));

        public OrderChangeStateReasonsView()
        {
            InitializeComponent();
        }

        public ObservableCollection<OrderStateChangeReasonViewItem> Reasons
        {
            get => (ObservableCollection<OrderStateChangeReasonViewItem>)GetValue(ReasonsProperty);
            set => SetValue(ReasonsProperty, value);
        }

        public OrderStateChangeReasonViewItem CurrentReason
        {
            get => (OrderStateChangeReasonViewItem)GetValue(CurrentReasonProperty);
            set => SetValue(CurrentReasonProperty, value);
        }

        private void TreeListViewOnCustomCellAppearance(object sender, CustomCellAppearanceEventArgs e)
        {
            if (e.CellSelectionState != SelectionState.None || e.RowSelectionState != SelectionState.None)
            {
                object result = e.OriginalValue;

                if (e.Property == TextBlock.BackgroundProperty || e.Property == TextBlock.ForegroundProperty)
                {
                    SolidColorBrush original = e.OriginalValue as SolidColorBrush;
                    SolidColorBrush conditional = e.ConditionalValue as SolidColorBrush;

                    if (conditional != null && (original == null || original.Color != conditional.Color))
                    {
                        result = conditional;
                    }
                }

                e.Result = result;
                e.Handled = true;
            }
        }
    }
}