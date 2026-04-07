using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace Telemart.Client.Controls
{
    public class TimerTextBlock : TextBlock
    {
        public static readonly DependencyProperty StateProperty = DependencyProperty.Register(
            nameof(State),
            typeof(TimerState),
            typeof(TimerTextBlock),
            new PropertyMetadata(TimerState.Stop, StatePropertyChangedCallback));

        private const string Format = @"hh\:mm\:ss";

        private DispatcherTimer timer;
        private TimeSpan time;

        public TimerTextBlock()
        {
            IsVisibleChanged += OnIsVisibleChanged;
            Unloaded += OnUnloaded;
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            RemoveTimer();
            IsVisibleChanged -= OnIsVisibleChanged;
            Unloaded -= OnUnloaded;
        }

        private void OnIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue is not true)
            {
                RemoveTimer();
            }
        }

        public TimerState State
        {
            get => (TimerState)GetValue(StateProperty);
            set => SetValue(StateProperty, value);
        }

        private DispatcherTimer GetOrCreateTimer()
        {
            if (timer == null)
            {
                timer = new DispatcherTimer
                {
                    Interval = new TimeSpan(0, 0, 1),
                };

                timer.Tick += TimerTick;

                time = TimeSpan.Zero;
                Text = time.ToString(Format);
            }

            return timer;
        }

        private void RemoveTimer()
        {
            if (timer == null)
            {
                return;
            }

            timer.Stop();
            timer.Tick -= TimerTick;
            timer = null;
        }

        private static void StatePropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue is TimerState state && d is TimerTextBlock timerTextBlock)
            {
                switch (state)
                {
                    case TimerState.Stop:
                        timerTextBlock.RemoveTimer();
                        break;
                    case TimerState.Pause:
                        timerTextBlock.timer?.Stop();
                        break;
                    case TimerState.Start:
                        timerTextBlock.GetOrCreateTimer().Start();
                        break;
                }
            }
        }

        private void TimerTick(object sender, EventArgs e)
        {
            time += timer.Interval;

            Text = time.ToString(Format);
        }
    }
}