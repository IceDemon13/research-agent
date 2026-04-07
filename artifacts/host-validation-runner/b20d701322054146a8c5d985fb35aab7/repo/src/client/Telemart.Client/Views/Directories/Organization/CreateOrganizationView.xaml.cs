using System;
using System.Windows.Threading;

namespace Telemart.Client.Views.Directories.Organization
{
    /// <summary>
    /// Interaction logic for CreateOrganizationView.xaml
    /// </summary>
    public partial class CreateOrganizationView
    {
        public CreateOrganizationView()
        {
            InitializeComponent();
        }

        private void CreateOrganizationViewLoaded(object sender, System.Windows.RoutedEventArgs e)
        {
            Dispatcher.BeginInvoke(DispatcherPriority.ContextIdle, new Action(() => OwnershipsComboBox.Focus()));
        }
    }
}
