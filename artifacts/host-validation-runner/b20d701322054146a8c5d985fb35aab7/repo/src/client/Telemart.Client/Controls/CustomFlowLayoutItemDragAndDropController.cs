using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using DevExpress.Xpf.Core;
using DevExpress.Xpf.Core.Native;
using DevExpress.Xpf.LayoutControl;

namespace Telemart.Client.Controls
{
    public sealed class CustomFlowLayoutItemDragAndDropController : FlowLayoutItemDragAndDropController
    {
        public CustomFlowLayoutItemDragAndDropController(Controller controller, Point startDragPoint, FrameworkElement dragControl)
            : base(controller, startDragPoint, dragControl)
        {
        }

        public void PerformFakeDragAndDrop(int newIndex)
        {
            if (ElementPositionsAnimation != null)
            {
                ElementPositionsAnimation.Stop();
                ElementPositionsAnimation = null;
            }

            if (Controller.ILayoutControl.AnimateItemMoving)
            {
                ElementPositionsAnimation = new ElementBoundsAnimation(new FrameworkElements { DragControl });
                ElementPositionsAnimation.StoreOldElementBounds(Controller.Control);
            }

            bool dropIsFlowBreak = FlowLayoutControl.GetIsFlowBreak(DragControlParent.Children[newIndex]);
            {
                if (DragControlIndex != newIndex || FlowLayoutControl.GetIsFlowBreak(DragControl) != dropIsFlowBreak)
                {
                    int oldPosition = Controller.ILayoutControl.GetLogicalChildren(false).IndexOf(DragControl);
                    DragControlParent.Children.RemoveAt(DragControlIndex);
                    DragControlParent.Children.Insert(newIndex, DragControl);
                    FlowLayoutControl.SetIsFlowBreak(DragControl, dropIsFlowBreak);
                    int newPosition = Controller.ILayoutControl.GetLogicalChildren(false).IndexOf(DragControl);
                    Controller.ILayoutControl.OnItemPositionChanged(oldPosition, newPosition);
                }

                SendIsFlowBreakChangeNotifications();
            }

            if (Controller.ILayoutControl.AnimateItemMoving)
            {
                object storedDragControlOpacity = DragControl.StorePropertyValue(UIElement.OpacityProperty);
                DragControl.Opacity = 0;
                Controller.Control.UpdateLayout();
                DragControl.RestorePropertyValue(UIElement.OpacityProperty, storedDragControlOpacity);
                ElementPositionsAnimation.StoreNewElementBounds();
                object storedDragControlZIndex = DragControl.StorePropertyValue(Panel.ZIndexProperty);
                DragControl.SetZIndex(PanelBase.HighZIndex);
                object storedDragControlIsHitTestVisible = DragControl.StorePropertyValue(UIElement.IsHitTestVisibleProperty);
                DragControl.IsHitTestVisible = false;

                ElementPositionsAnimation.Begin(
                    FlowLayoutControl.ItemDropAnimationDuration,
                    new ExponentialEase { Exponent = 5 },
                    () =>
                    {
                        DragControl.RestorePropertyValue(Panel.ZIndexProperty, storedDragControlZIndex);
                        DragControl.RestorePropertyValue(UIElement.IsHitTestVisibleProperty, storedDragControlIsHitTestVisible);
                        ElementPositionsAnimation = null;
                        (Controller.ILayoutControl as CustomFlowLayoutControl).RaiseAnimationCompleted();
                    });
            }
        }
    }
}