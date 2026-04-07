using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using DevExpress.Xpf.Grid;

namespace Telemart.Client.Views.Warehouse.Movement
{
    /// <summary>
    /// Interaction logic for CreateMovementView.xaml
    /// </summary>
    public partial class CreateMovementView
    {
        public CreateMovementView()
        {
            InitializeComponent();
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            Dispatcher.BeginInvoke(DispatcherPriority.ContextIdle, new Action(() => FromWarehouseComboBoxEdit.Focus()));
        }

        private void TableViewOnCustomRowAppearance(object sender, CustomRowAppearanceEventArgs e)
        {
            if (e.RowSelectionState == SelectionState.None)
            {
                return;
            }

            object result = e.ConditionalValue;

            if (e.Property == TextBlock.ForegroundProperty)
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
