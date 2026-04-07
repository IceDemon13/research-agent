using System.Windows;
using DevExpress.Xpf.Core;
using DevExpress.Xpf.LayoutControl;

namespace Telemart.Client.Controls
{
    public sealed class CustomFlowLayoutController : FlowLayoutController
    {
        public CustomFlowLayoutController(IFlowLayoutControl control)
            : base(control)
        {
        }

        public void StartFakeDragAndDrop(int oldIndex, int newIndex)
        {
            CustomFlowLayoutItemDragAndDropController controller = new CustomFlowLayoutItemDragAndDropController(
                this,
                default(Point),
                ((CustomFlowLayoutControl)IControl).Children[oldIndex] as FrameworkElement);

            controller.PerformFakeDragAndDrop(newIndex);
        }

        protected override DragAndDropController CreateItemDragAndDropControler(Point startDragPoint, FrameworkElement dragControl)
        {
            return new CustomFlowLayoutItemDragAndDropController(this, startDragPoint, dragControl);
        }
    }
}