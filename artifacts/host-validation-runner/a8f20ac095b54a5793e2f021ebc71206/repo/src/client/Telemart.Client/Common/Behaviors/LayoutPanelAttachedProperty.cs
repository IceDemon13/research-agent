using System.Windows;
using DevExpress.Xpf.Docking;

namespace Telemart.Client.Common.Behaviors
{
    internal sealed class LayoutPanelAttachedProperty
    {
        public static readonly DependencyProperty ShowProperty = DependencyProperty.RegisterAttached(
            "Show",
            typeof(bool),
            typeof(LayoutPanelAttachedProperty),
            new PropertyMetadata(default(bool), OnShowPropertyChanged));

        public static bool GetShow(DependencyObject target)
        {
            return (bool)target.GetValue(ShowProperty);
        }

        public static void SetShow(DependencyObject target, bool value)
        {
            target.SetValue(ShowProperty, value);
        }

        private static void OnShowPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            LayoutPanel panel = sender as LayoutPanel;

            DockLayoutManager dockLayoutManager = panel?.GetDockLayoutManager();

            if (dockLayoutManager?.DockController == null)
            {
                return;
            }

            if ((bool)e.NewValue)
            {
                if (panel.IsAutoHidden)
                {
                    if (panel.IsActive)
                    {
                        dockLayoutManager.DockController.Restore(panel);
                    }
                    else
                    {
                        dockLayoutManager.DockController.Dock(panel);
                    }
                }
            }
            else
            {
                if (panel.IsVisible)
                {
                    dockLayoutManager.DockController.Hide(panel);
                }
            }
        }
    }
}