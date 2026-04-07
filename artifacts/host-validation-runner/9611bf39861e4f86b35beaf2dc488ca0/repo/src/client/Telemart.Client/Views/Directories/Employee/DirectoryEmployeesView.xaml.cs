using System.Windows.Controls;
using DevExpress.Xpf.Editors;

namespace Telemart.Client.Views.Directories.Employee
{
    /// <summary>
    /// Interaction logic for DirectoryEmployeesView.xaml.
    /// </summary>
    public partial class DirectoryEmployeesView : UserControl
    {
        public DirectoryEmployeesView()
        {
            InitializeComponent();
        }

        private void Telegram_OnRequestNavigation(object sender, HyperlinkEditRequestNavigationEventArgs e)
        {
            e.NavigationUrl = $"https://t.me/{e.NavigationUrl}/";
            e.Handled = true;
        }

        private void Skype_OnRequestNavigation(object sender, HyperlinkEditRequestNavigationEventArgs e)
        {
            e.NavigationUrl = $"skype:{e.NavigationUrl}?chat";
            e.Handled = true;
        }
    }
}
