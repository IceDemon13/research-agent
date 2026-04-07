using System;
using System.Windows.Threading;
using DevExpress.Xpf.Grid;

namespace Telemart.Client.Common.Behaviors
{
    public class AutoUpdateBehaviour : BehaviorBase<GridControl>
    {
        private DispatcherTimer timer;

        public TimeSpan Interval { get; set; }

        protected override void OnSetup()
        {
            base.OnSetup();

            timer = new DispatcherTimer(Interval, DispatcherPriority.Normal, Callback, Dispatcher.CurrentDispatcher);
        }

        protected override void OnCleanup()
        {
            base.OnCleanup();

            timer?.Stop();
            timer = null;
        }

        private void Callback(object sender, EventArgs e)
        {
            AssociatedObject.RefreshData();
        }
    }
}
