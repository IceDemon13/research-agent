using System.Collections.Generic;
using System.Windows;
using DevExpress.Xpf.Editors;

namespace Telemart.Client.Views.Directories.Contractor.ParserSettings
{
    public partial class ParserSettingsCategoryView
    {
        public ParserSettingsCategoryView()
        {
            InitializeComponent();
        }

        private void ComboboxEditOnProcessNewValue(DependencyObject sender, ProcessNewValueEventArgs e)
        {
            ComboBoxEdit edit = (ComboBoxEdit)e.Source;

            ICollection<string> items = (ICollection<string>)edit.ItemsSource;

            items.Add(e.DisplayText);

            e.PostponedValidation = true;
            e.Handled = true;
        }

        private void ComboboxEditOnValidate(object sender, ValidationEventArgs e)
        {
            e.IsValid = true;
        }
    }
}
