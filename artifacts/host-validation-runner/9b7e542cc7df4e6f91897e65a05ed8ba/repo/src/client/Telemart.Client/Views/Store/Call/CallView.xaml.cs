using System;
using System.Windows;
using System.Windows.Threading;
using Telemart.Client.Common;

namespace Telemart.Client.Views.Store.Call
{
    /// <summary>
    /// Interaction logic for CallView.xaml
    /// </summary>
    public partial class CallView
    {
        public CallView()
        {
            InitializeComponent();

            DataObject.AddCopyingHandler(PhoneEdit, PhoneCopingToClipboardHelper.Handle);
            DataObject.AddCopyingHandler(Phone2Edit, PhoneCopingToClipboardHelper.Handle);
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            Dispatcher.BeginInvoke(DispatcherPriority.ContextIdle, new Action(() => PriorityComboBox.Focus()));
        }
    }
}
