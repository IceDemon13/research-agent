using System;
using System.Windows.Threading;

namespace Telemart.Client.Views.Directories.Organization
{
    /// <summary>
    /// Interaction logic for OrganizationView.xaml
    /// </summary>
    public partial class OrganizationView
    {
        public OrganizationView()
        {
            InitializeComponent();
        }

        private void OrganizationViewLoaded(object sender, System.Windows.RoutedEventArgs e)
        {
            Dispatcher.BeginInvoke(DispatcherPriority.ContextIdle, new Action(() => OwnershipsComboBox.Focus()));
        }
    }
}
