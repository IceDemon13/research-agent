using System.Windows;

namespace Telemart.Client.ViewComponents.Threading
{
    /// <summary>
    /// Interaction logic for BusyUserControl.xaml
    /// </summary>
    public partial class BusyIndicator
    {
        public static readonly DependencyProperty BusyTextProperty = DependencyProperty.Register(
            "BusyText",
            typeof(string),
            typeof(BusyIndicator),
            new PropertyMetadata(default(string)));

        public BusyIndicator()
        {
            InitializeComponent();
        }

        public string BusyText
        {
            get => (string)GetValue(BusyTextProperty);
            set => SetValue(BusyTextProperty, value);
        }
    }
}
