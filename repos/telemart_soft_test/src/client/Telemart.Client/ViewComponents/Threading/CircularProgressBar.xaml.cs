using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace Telemart.Client.ViewComponents.Threading
{
    /// <summary>
    /// Interaction logic for CircularProgressBar.xaml
    /// </summary>
    public partial class CircularProgressBar
    {
        private readonly DispatcherTimer animationTimer;

        public CircularProgressBar()
        {
            InitializeComponent();

            animationTimer = new DispatcherTimer(DispatcherPriority.Background, Dispatcher)
            {
                Interval = TimeSpan.FromMilliseconds(75)
            };
        }

        private void Start()
        {
            Mouse.OverrideCursor = Cursors.Wait;
            animationTimer.Tick += HandleAnimationTick;
            animationTimer.Start();
        }

        private void Stop()
        {
            animationTimer.Stop();
            Mouse.OverrideCursor = Cursors.Arrow;
            animationTimer.Tick -= HandleAnimationTick;
        }

        private void HandleAnimationTick(object sender, EventArgs e)
        {
            SpinnerRotate.Angle = (SpinnerRotate.Angle + 36) % 360;
        }

        private void HandleLoaded(object sender, RoutedEventArgs e)
        {
            const double Offset = Math.PI;
            const double Step = Math.PI * 2 / 10.0;

            SetPosition(C0, Offset, 0.0, Step);
            SetPosition(C1, Offset, 1.0, Step);
            SetPosition(C2, Offset, 2.0, Step);
            SetPosition(C3, Offset, 3.0, Step);
            SetPosition(C4, Offset, 4.0, Step);
            SetPosition(C5, Offset, 5.0, Step);
            SetPosition(C6, Offset, 6.0, Step);
            SetPosition(C7, Offset, 7.0, Step);
            SetPosition(C8, Offset, 8.0, Step);
        }

        private void SetPosition(Ellipse ellipse, double offset, double posOffSet, double step)
        {
            double left = 50.0 + (Math.Sin(offset + (posOffSet * step)) * 50.0);
            double top = 50 + (Math.Cos(offset + (posOffSet * step)) * 50.0);

            ellipse.SetValue(Canvas.LeftProperty, left);
            ellipse.SetValue(Canvas.TopProperty, top);
        }

        private void HandleUnloaded(object sender, RoutedEventArgs e)
        {
            Stop();
        }

        private void HandleVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            bool isVisible = (bool)e.NewValue;

            if (isVisible)
            {
                Start();
            }
            else
            {
                Stop();
            }
        }
    }
}
