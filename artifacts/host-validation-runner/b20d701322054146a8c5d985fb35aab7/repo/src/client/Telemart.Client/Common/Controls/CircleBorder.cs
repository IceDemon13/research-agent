using System.Windows;
using System.Windows.Controls;

namespace Telemart.Client.Common.Controls
{
    public class CircleBorder : Border
    {
        public static readonly DependencyProperty RadiusProperty =
            DependencyProperty.Register("Radius", typeof(double), typeof(CircleBorder), new PropertyMetadata(0d, new PropertyChangedCallback(OnRadiusChanged)));

        public double Radius
        {
            get { return (double)GetValue(RadiusProperty); }
            set { SetValue(RadiusProperty, value); }
        }

        private static void OnRadiusChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            double radius = (double)e.NewValue;

            d.SetValue(CornerRadiusProperty, new CornerRadius(radius));

            d.SetValue(WidthProperty, radius * 2);
            d.SetValue(HeightProperty, radius * 2);

            d.SetValue(MarginProperty, new Thickness(-radius, -radius, 0, 0));
        }
    }
}
