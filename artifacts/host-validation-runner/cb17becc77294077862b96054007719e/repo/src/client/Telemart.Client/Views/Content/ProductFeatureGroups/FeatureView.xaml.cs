using System;
using System.Windows.Threading;

namespace Telemart.Client.Views.Content.ProductFeatureGroups
{
    /// <summary>
    /// Interaction logic for FeatureView.xaml
    /// </summary>
    public partial class FeatureView
    {
        public FeatureView()
        {
            InitializeComponent();
        }

        private void OnLoaded(object sender, System.Windows.RoutedEventArgs e)
        {
            Dispatcher.BeginInvoke(DispatcherPriority.ContextIdle, new Action(() => NameTextEdit.Focus()));
        }
    }
}
