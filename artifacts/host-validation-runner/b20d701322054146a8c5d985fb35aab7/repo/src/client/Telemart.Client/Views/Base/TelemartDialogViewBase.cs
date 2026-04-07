using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Telemart.Client.Views.Base
{
    public class TelemartDialogViewBase : UserControl
    {
        public static readonly DependencyProperty CancelKeyProperty =
           DependencyProperty.Register("CancelKey", typeof(KeyGesture), typeof(TelemartDialogViewBase), new PropertyMetadata(new KeyGesture(Key.Escape)));

        public static readonly DependencyProperty OkKeyProperty =
            DependencyProperty.Register("OkKey", typeof(KeyGesture), typeof(TelemartDialogViewBase), new PropertyMetadata(new KeyGesture(Key.Enter, ModifierKeys.Control)));

        public static readonly DependencyProperty BusyTextProperty =
            DependencyProperty.Register("BusyText", typeof(string), typeof(TelemartDialogViewBase), new PropertyMetadata(string.Empty));

        static TelemartDialogViewBase()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(TelemartDialogViewBase), new FrameworkPropertyMetadata(typeof(TelemartDialogViewBase)));
        }

        public KeyGesture CancelKey
        {
            get { return (KeyGesture)GetValue(CancelKeyProperty); }
            set { SetValue(CancelKeyProperty, value); }
        }

        public KeyGesture OkKey
        {
            get { return (KeyGesture)GetValue(OkKeyProperty); }
            set { SetValue(OkKeyProperty, value); }
        }

        public string BusyText
        {
            get { return (string)GetValue(BusyTextProperty); }
            set { SetValue(BusyTextProperty, value); }
        }
    }
}
