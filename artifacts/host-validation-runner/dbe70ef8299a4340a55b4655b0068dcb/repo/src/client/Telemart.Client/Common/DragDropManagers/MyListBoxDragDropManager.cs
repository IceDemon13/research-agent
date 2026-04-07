using System;
using System.Collections;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using DevExpress.Xpf.Core.Native;
using DevExpress.Xpf.Editors;
using DevExpress.Xpf.Grid;
using DevExpress.Xpf.Grid.DragDrop;

namespace Telemart.Client.Common.DragDropManagers
{
    public class MyListBoxDragDropManager : ListBoxDragDropManager
    {
        private ListBoxEditItem targetItem;

        protected override IList ItemsSource => ListBox.ItemsSource is ICollectionView view
            ? view.SourceCollection as IList
            : ListBox.ItemsSource as IList;

        public override void OnDragOver(DragDropManagerBase sourceManager, UIElement source, Point point)
        {
            ListBoxDragOverEventArgs e = RaiseDragOverEvent(sourceManager, point, DropTargetType.None);

            DropEventIsLocked = e.Handled
                ? !e.AllowDrop
                : !AllowDrop || sourceManager.DraggingRows == null || sourceManager.DraggingRows.Count < 1;

            if (!DropEventIsLocked)
            {
                ProcessTargetItem(sourceManager, point);
            }
        }

        protected virtual ListBoxEditItem GetVisibleHitTestElement(Point point)
        {
            DragDropHitTestResult result = new DragDropHitTestResult();

            VisualTreeHelper.HitTest(ListBox, null, result.CallBack, new PointHitTestParameters(point));

            return result.Element;
        }

        protected override void OnDrop(DragDropManagerBase sourceManager, UIElement source, Point point)
        {
            if (DropEventIsLocked)
            {
                return;
            }

            ListBoxDropEventArgs e = RaiseDropEvent(sourceManager);

            if (!e.Handled)
            {
                if (sourceManager.DraggingRows.Count > 0 && AllowDrop)
                {
                    ProcessTargetItem(e.SourceManager, point);

                    IList currentSource = ItemsSource;
                    IList draggedFromSource = e.SourceManager.GetSource(null);

                    foreach (object obj in e.DraggedRows)
                    {
                        if (targetItem != null && obj == targetItem.Content)
                        {
                            continue;
                        }

                        draggedFromSource.Remove(obj);

                        switch (e.SourceManager.ViewInfo.DropTargetType)
                        {
                            case DropTargetType.DataArea:
                                currentSource.Add(obj);
                                break;
                            case DropTargetType.InsertRowsAfter:
                                currentSource.Insert(currentSource.IndexOf(targetItem.Content) + 1, obj);
                                break;
                            case DropTargetType.InsertRowsBefore:
                                currentSource.Insert(Math.Max(currentSource.IndexOf(targetItem.Content), 0), obj);
                                break;
                        }
                    }

                    RaiseDroppedEvent(sourceManager, e.DraggedRows);
                }
            }

            HideDropMarker();
        }

        protected virtual void ProcessTargetItem(DragDropManagerBase sourceManager, Point pt)
        {
            targetItem = GetVisibleHitTestElement(pt);

            if (targetItem == null)
            {
                sourceManager.ViewInfo.DropTargetRow = null;
                sourceManager.SetDropTargetType(DropTargetType.DataArea);
                ShowDropMarker(ListBox, TableDragIndicatorPosition.None);
                return;
            }

            Point position = Mouse.GetPosition(targetItem);

            double height = targetItem.ActualHeight;

            if (position.Y < height / 2)
            {
                sourceManager.SetDropTargetType(DropTargetType.InsertRowsBefore);
                ShowDropMarker(targetItem, TableDragIndicatorPosition.Top);
            }
            else
            {
                sourceManager.SetDropTargetType(DropTargetType.InsertRowsAfter);
                ShowDropMarker(targetItem, TableDragIndicatorPosition.Bottom);
            }

            sourceManager.ViewInfo.DropTargetRow = targetItem.Content;
        }

        private class DragDropHitTestResult
        {
            public ListBoxEditItem Element { get; private set; }

            public HitTestResultBehavior CallBack(HitTestResult result)
            {
                ListBoxEditItem item = LayoutHelper.FindParentObject<ListBoxEditItem>(result.VisualHit);

                HitTestResultBehavior hitTestResultBehavior;

                if (item != null)
                {
                    Element = item;
                    hitTestResultBehavior = HitTestResultBehavior.Stop;
                }
                else
                {
                    hitTestResultBehavior = HitTestResultBehavior.Continue;
                }

                return hitTestResultBehavior;
            }
        }
    }
}