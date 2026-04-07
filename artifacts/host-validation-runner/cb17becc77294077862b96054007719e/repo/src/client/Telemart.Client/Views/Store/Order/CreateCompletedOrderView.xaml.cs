using System.Windows;
using System.Windows.Input;
using DevExpress.Xpf.Editors;
using Telemart.Client.Core.Extensions;

namespace Telemart.Client.Views.Store.Order
{
    /// <summary>
    /// Interaction logic for CreateCompletedOrderView.xaml
    /// </summary>
    public partial class CreateCompletedOrderView
    {
        public CreateCompletedOrderView()
        {
            InitializeComponent();
        }

        private void Phone1_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.V && Keyboard.Modifiers == ModifierKeys.Control)
            {
                if (sender is ButtonEdit buttonEdit)
                {
                    buttonEdit.EditValue = Clipboard.GetText().GetLocalPhoneNumber();

                    e.Handled = true;
                }
            }
        }

        private void Phone2_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.V && Keyboard.Modifiers == ModifierKeys.Control)
            {
                if (sender is TextEdit textEdit)
                {
                    textEdit.EditValue = Clipboard.GetText().GetLocalPhoneNumber();

                    e.Handled = true;
                }
            }
        }
    }
}