using System;
using System.Windows.Threading;

namespace Telemart.Client.Views.Common
{
    /// <summary>
    /// Interaction logic for ChooseAbcClassView.xaml
    /// </summary>
    public partial class ChooseAbcClassView
    {
        public ChooseAbcClassView()
        {
            InitializeComponent();
        }

        private void ChooseAbcTypesLoaded(object sender, System.Windows.RoutedEventArgs e)
        {
            Dispatcher.BeginInvoke(DispatcherPriority.ContextIdle, new Action(() => AbcTypesComboBox.Focus()));
        }
    }
}
