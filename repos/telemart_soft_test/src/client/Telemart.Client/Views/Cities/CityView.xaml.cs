using DevExpress.Xpf.Editors;

namespace Telemart.Client.Views.Cities
{
    /// <summary>
    /// Interaction logic for CityView.xaml
    /// </summary>
    public partial class CityView
    {
        public CityView()
        {
            InitializeComponent();
        }

        private void CityComboBoxEditOnPopupOpening(object sender, OpenPopupEventArgs e)
        {
            e.Cancel = true;
        }
    }
}
