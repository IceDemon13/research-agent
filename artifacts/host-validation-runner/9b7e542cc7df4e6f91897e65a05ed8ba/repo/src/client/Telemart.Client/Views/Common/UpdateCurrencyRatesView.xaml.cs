using System;
using System.Windows;
using System.Windows.Threading;
using DevExpress.Xpf.Grid;

namespace Telemart.Client.Views.Common
{
    /// <summary>
    /// Interaction logic for UpdateCurrencyRatesView.xaml
    /// </summary>
    public partial class UpdateCurrencyRatesView
    {
        public UpdateCurrencyRatesView()
        {
            InitializeComponent();
        }

        private void TableView_CustomCellAppearance(object sender, CustomCellAppearanceEventArgs e)
        {
            e.Result = e.ConditionalValue;
            e.Handled = true;
        }
    }
}