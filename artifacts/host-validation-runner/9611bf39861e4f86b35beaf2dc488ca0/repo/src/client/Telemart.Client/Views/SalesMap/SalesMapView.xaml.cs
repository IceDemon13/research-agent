using System.Windows.Controls;
using System.Windows.Input;
using DevExpress.Xpf.Map;

namespace Telemart.Client.Views.SalesMap
{
    /// <summary>
    /// Interaction logic for SalesMapView.xaml
    /// </summary>
    public partial class SalesMapView : UserControl
    {
        public SalesMapView()
        {
            InitializeComponent();
        }

        private void Map_MouseDown(object sender, MouseButtonEventArgs e)
        {
            object[] objects = Map.CalcHitInfo(e.GetPosition(Map)).HitObjects;

            if (objects.Length > 0)
            {
                Map.ZoomToFit(((MapItem)objects[0]).ClusteredItems);
            }
        }
    }
}
