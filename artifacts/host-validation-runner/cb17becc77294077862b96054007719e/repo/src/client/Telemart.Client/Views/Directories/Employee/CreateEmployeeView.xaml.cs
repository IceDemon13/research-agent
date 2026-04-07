using System.Windows.Controls;
using DevExpress.Xpf.Editors;

namespace Telemart.Client.Views.Directories.Employee
{
    /// <summary>
    /// Interaction logic for CreateEmployeeView.xaml
    /// </summary>
    public partial class CreateEmployeeView : UserControl
    {
        public CreateEmployeeView()
        {
            InitializeComponent();
        }

        private void BaseEditOnValidate(object sender, ValidationEventArgs e)
        {
            e.IsValid = true;
        }
    }
}
