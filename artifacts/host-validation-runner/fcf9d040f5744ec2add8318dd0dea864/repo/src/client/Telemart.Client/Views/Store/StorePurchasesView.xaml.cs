using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using DevExpress.Xpf.Grid;
using Telemart.Client.ViewModels.Directories.Category;
using Telemart.Client.ViewModels.Store;

namespace Telemart.Client.Views.Store
{
    /// <summary>
    ///     Interaction logic for StorePurchasesView.xaml
    /// </summary>
    public partial class StorePurchasesView
    {
        public StorePurchasesView()
        {
            InitializeComponent();

            StorePurchasesViewModel viewModel = (StorePurchasesViewModel)DataContext;
            viewModel.OnPurchaseSourceUpdated += OnPurchaseSourceUpdated;
        }

        private void OnPurchaseSourceUpdated()
        {
            Dispatcher.BeginInvoke(DispatcherPriority.ContextIdle, new Action(() => PurchasesTableView.Focus()));
        }

        private void PurchasesGridLoaded(object sender, RoutedEventArgs e)
        {
            PurchasesGrid.FilterString = string.Empty;
        }

        private void PurchasesTableViewOnCustomCellAppearance(object sender, CustomCellAppearanceEventArgs e)
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