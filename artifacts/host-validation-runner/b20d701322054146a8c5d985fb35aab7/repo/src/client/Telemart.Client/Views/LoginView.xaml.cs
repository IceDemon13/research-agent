using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace Telemart.Client.Views
{
    /// <summary>
    /// Interaction logic for LoginView.xaml
    /// </summary>
    public partial class LoginView
    {
        public LoginView()
        {
            InitializeComponent();
        }

        private void LoginViewOnLoaded(object sender, RoutedEventArgs e)
        {
            Dispatcher.BeginInvoke(DispatcherPriority.ContextIdle, new Action(() => LoginTextBox.Focus()));
        }

        private void LoginTextBoxOnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                Dispatcher.BeginInvoke(DispatcherPriority.ContextIdle, new Action(() => PasswordBoxEdit.Focus()));
            }
        }
    }
}
