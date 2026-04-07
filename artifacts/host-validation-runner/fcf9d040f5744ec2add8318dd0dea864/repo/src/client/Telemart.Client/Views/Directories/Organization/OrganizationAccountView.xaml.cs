using System;
using System.Windows;
using System.Windows.Threading;

namespace Telemart.Client.Views.Directories.Organization
{
    /// <summary>
    /// Interaction logic for OrganizationAccountView.xaml
    /// </summary>
    public partial class OrganizationAccountView
    {
        public OrganizationAccountView()
        {
            InitializeComponent();
        }

        private void OrganizationAccountViewLoaded(object sender, RoutedEventArgs e)
        {
            Dispatcher.BeginInvoke(DispatcherPriority.ContextIdle, new Action(() => NameTextEdit.Focus()));
        }
    }
}
