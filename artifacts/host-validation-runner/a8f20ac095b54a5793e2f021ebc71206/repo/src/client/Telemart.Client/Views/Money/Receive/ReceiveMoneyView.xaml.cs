using System.Windows.Controls;
using System.Windows.Media;
using DevExpress.Xpf.Grid;

namespace Telemart.Client.Views.Money.Receive
{
    /// <summary>
    /// Interaction logic for ReceiveMoneyView.xaml
    /// </summary>
    public partial class ReceiveMoneyView
    {
        public ReceiveMoneyView()
        {
            InitializeComponent();
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
