using System.Windows;
using System.Windows.Controls;
using Telemart.Client.Common;

namespace Telemart.Client.Views.Service.ServiceRequests
{
    /// <summary>
    /// Interaction logic for ServiceRequestView.xaml
    /// </summary>
    public partial class ServiceRequestView
    {
        public ServiceRequestView()
        {
            InitializeComponent();

            DataObject.AddCopyingHandler(PhoneEdit, PhoneCopingToClipboardHelper.Handle);
            DataObject.AddCopyingHandler(Phone2Edit, PhoneCopingToClipboardHelper.Handle);
        }

        private void ButtonIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (sender is Button button && e.NewValue is bool newValue && newValue)
            {
                button.Focus();
            }
        }
    }
}
