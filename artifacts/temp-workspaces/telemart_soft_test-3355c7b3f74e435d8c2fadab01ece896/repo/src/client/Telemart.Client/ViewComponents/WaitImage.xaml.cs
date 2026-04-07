using System.Windows;

namespace Telemart.Client.ViewComponents
{
    /// <summary>
    /// Interaction logic for WaitImage.xaml
    /// </summary>
    public partial class WaitImage
    {
        public static readonly DependencyProperty BusyTextProperty = DependencyProperty.Register(
            "BusyText",
            typeof(string),
            typeof(WaitImage),
            new PropertyMetadata(default(string)));

        public WaitImage()
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
