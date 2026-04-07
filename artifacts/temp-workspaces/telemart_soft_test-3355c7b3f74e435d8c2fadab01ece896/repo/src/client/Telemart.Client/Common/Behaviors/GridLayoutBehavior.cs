using System.IO;
using System.Windows;
using DevExpress.Xpf.Grid;

namespace Telemart.Client.Common.Behaviors
{
    internal sealed class GridLayoutBehavior : BehaviorBase<GridControl>
    {
        public static readonly DependencyProperty LayoutProperty = DependencyProperty.Register(
            "Layout",
            typeof(string),
            typeof(GridLayoutBehavior),
            new PropertyMetadata(OnLayoutChanged));

        public string Layout
        {
            get => (string)GetValue(LayoutProperty);
            set => SetValue(LayoutProperty, value);
        }

        protected override void OnSetup()
        {
            OnLayoutChanged();
        }

        private static void OnLayoutChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            GridLayoutBehavior behavior = d as GridLayoutBehavior;
            behavior?.OnLayoutChanged();
        }

        private void OnLayoutChanged()
        {
            if (!string.IsNullOrWhiteSpace(Layout) && AssociatedObject != null)
            {
                using MemoryStream stream = new MemoryStream();
                using StreamWriter writer = new StreamWriter(stream);
                writer.Write(Layout);
                writer.Flush();

                stream.Position = 0;

                AssociatedObject.RestoreLayoutFromStream(stream);
            }
        }
    }
}