using System.Windows;
using DevExpress.Xpf.Core;
using DevExpress.Xpf.LayoutControl;

namespace Telemart.Client.Controls
{
    public sealed class CustomFlowLayoutControl : FlowLayoutControl
    {
        public event RoutedEventHandler AnimationCompleted;

        public bool DragInProcess { get; set; }

        public void RaiseAnimationCompleted()
        {
            DragInProcess = false;

            AnimationCompleted?.Invoke(this, new RoutedEventArgs());
        }

        public void StartFakeDragAndDrop(int oldIndex, int newIndex)
        {
            DragInProcess = true;

            ((CustomFlowLayoutController)Controller).StartFakeDragAndDrop(oldIndex, newIndex);
        }

        protected override PanelControllerBase CreateController()
        {
            return new CustomFlowLayoutController(this);
        }
    }
}