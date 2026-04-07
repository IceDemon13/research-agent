using System.Windows.Input;
using DevExpress.Xpf.Editors;

namespace Telemart.Client.Views.Directories.Category
{
    /// <summary>
    /// Interaction logic for CategoryOptionsView.xaml
    /// </summary>
    public partial class CategoryOptionsView
    {
        public CategoryOptionsView()
        {
            InitializeComponent();
        }

        ////private void TextEditMouseDoubleClick(object sender, MouseButtonEventArgs e)
        ////{
        ////    ContentPresenter presenter = e.OriginalSource as ContentPresenter;
        ////    TextEdit textEdit = presenter?.Content as TextEdit;

        ////    if (textEdit == null)
        ////    {
        ////        return;
        ////    }

        ////    TextBox textBox = GetInnerTextBox(textEdit);

        ////    textEdit.Focusable = true;
        ////    textEdit.IsEnabled = true;

        ////    if (textBox == null)
        ////    {
        ////        return;
        ////    }

        ////    textBox.Focusable = true;
        ////    textEdit.IsEnabled = true;
        ////    textBox.Focus();
        ////    textBox.Select(textEdit.Text.Length, 0);

        ////    IInputElement res = Keyboard.Focus(textBox);
        ////}

        ////private TextBox GetInnerTextBox(TextEdit textEdit)
        ////{
        ////    if (textEdit == null)
        ////    {
        ////        return null;
        ////    }

        ////    TextBox textBox = null;

        ////    int childrenCount = VisualTreeHelper.GetChildrenCount(textEdit);

        ////    if (childrenCount > 1)
        ////    {
        ////        textBox = VisualTreeHelper.GetChild(textEdit, 1) as TextBox;
        ////    }
        ////    else if(childrenCount == 1)
        ////    {
        ////        textBox = VisualTreeHelper.GetChild(textEdit, 0) as TextBox;
        ////    }

        ////    return textBox;
        ////}

        ////private void TextEditLostFocus(object sender, RoutedEventArgs e)
        ////{
        ////    var control = sender as TextEdit;

        ////    if (control != null)
        ////    {
        ////        control.IsEnabled = false;
        ////    }
        ////}

        private void ComboBoxEditValueChanged(object sender, EditValueChangedEventArgs e)
        {
            ComboBoxEdit editor = sender as ComboBoxEdit;
            editor?.MoveFocus(new TraversalRequest(FocusNavigationDirection.Left));
        }
    }
}
