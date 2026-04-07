using System;
using System.Windows;
using System.Windows.Threading;

namespace Telemart.Client.Views.Nomenclature
{
    /// <summary>
    /// Interaction logic for NomenclatureViewEx.xaml
    /// </summary>
    public partial class NomenclatureView
    {
        public NomenclatureView()
        {
            InitializeComponent();
        }

        private void NomenclatureViewExOnLoaded(object sender, RoutedEventArgs e)
        {
            Dispatcher.BeginInvoke(DispatcherPriority.ContextIdle, new Action(() => FilterSearchBox.Focus()));
        }
    }
}
