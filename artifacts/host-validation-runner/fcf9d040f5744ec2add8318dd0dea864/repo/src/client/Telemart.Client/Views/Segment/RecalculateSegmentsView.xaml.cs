using System.Windows.Controls;
using System.Windows.Media;
using DevExpress.Xpf.Grid;

namespace Telemart.Client.Views.Segment
{
    public partial class RecalculateSegmentsView
    {
        public RecalculateSegmentsView()
        {
            InitializeComponent();
        }

        private void TableViewOnCustomCellAppearance(object sender, CustomCellAppearanceEventArgs e)
        {
            if (e.CellSelectionState != SelectionState.None || e.RowSelectionState != SelectionState.None)
            {
                object result = e.OriginalValue;

                if (e.Property == TextBlock.BackgroundProperty || e.Property == TextBlock.ForegroundProperty)
                {
                    if (e.ConditionalValue is SolidColorBrush conditional && (e.OriginalValue is not SolidColorBrush original || original.Color != conditional.Color))
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