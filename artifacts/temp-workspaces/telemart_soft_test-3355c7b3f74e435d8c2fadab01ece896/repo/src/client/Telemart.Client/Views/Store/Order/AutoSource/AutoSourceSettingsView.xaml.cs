using System.Windows.Controls;
using System.Windows.Media;
using DevExpress.Xpf.Grid;

namespace Telemart.Client.Views.Store.Order.AutoSource
{
    public partial class AutoSourceSettingsView : UserControl
    {
        public AutoSourceSettingsView()
        {
            InitializeComponent();
        }

        private void TableView_OnCustomCellAppearance(object sender, CustomCellAppearanceEventArgs e)
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