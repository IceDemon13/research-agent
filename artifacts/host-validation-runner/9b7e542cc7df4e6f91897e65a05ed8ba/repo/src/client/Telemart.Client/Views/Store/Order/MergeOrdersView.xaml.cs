using System;
using System.Windows.Threading;

namespace Telemart.Client.Views.Store.Order
{
    /// <summary>
    /// Interaction logic for MergeOrdersView.xaml
    /// </summary>
    public partial class MergeOrdersView
    {
        public MergeOrdersView()
        {
            InitializeComponent();
        }

        private void LayoutControlIsVisibleChanged(object sender, System.Windows.DependencyPropertyChangedEventArgs e)
        {
            if ((bool)e.NewValue)
            {
                Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(() => InfoBlock.Focus()));
            }
        }
    }
}
