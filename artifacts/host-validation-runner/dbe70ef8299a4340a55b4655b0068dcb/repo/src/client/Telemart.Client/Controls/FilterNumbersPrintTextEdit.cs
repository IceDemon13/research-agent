using System.Windows;
using DevExpress.Xpf.Editors;
using DevExpress.Xpf.Grid;
using Telemart.Client.Extensions;

namespace Telemart.Client.Controls
{
    public class FilterNumbersPrintTextEdit : PrintTextEdit
    {
        public FilterNumbersPrintTextEdit()
        {
            AcceptsReturn = true;
            Loaded += (_, _) =>
            {
                EditValueChanging -= OnEditValueChanging;
                EditValueChanging += OnEditValueChanging;

                Unloaded -= OnUnLoad;
                Unloaded += OnUnLoad;
            };
        }

        private void OnEditValueChanging(object sender, EditValueChangingEventArgs e)
        {
            if (sender is PrintTextEdit textEdit)
            {
                string settingValue = e.NewValue as string;

                settingValue = settingValue?.ChangeToCommaValue();

                textEdit.EditValue = settingValue;

                if (settingValue?.Length > 0 && textEdit.CaretIndex == 0)
                {
                    textEdit.CaretIndex = settingValue.Length;
                }

                e.Handled = true;
            }
        }

        private void OnUnLoad(object sender, RoutedEventArgs args)
        {
            EditValueChanging -= OnEditValueChanging;
            Unloaded -= OnUnLoad;
        }
    }
}