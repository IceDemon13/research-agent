using System.IO;
using System.Windows;
using DevExpress.Xpf.PivotGrid;

namespace Telemart.Client.Common.Behaviors
{
    internal sealed class PivotGridLayoutBehavior : BehaviorBase<PivotGridControl>
    {
        public static readonly DependencyProperty LayoutProperty = DependencyProperty.Register(
            "Layout",
            typeof(string),
            typeof(PivotGridLayoutBehavior),
            new PropertyMetadata(OnLayoutChanged));

        public string Layout
        {
            get => (string)GetValue(LayoutProperty);
            set => SetValue(LayoutProperty, value);
        }

        private static void OnLayoutChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            PivotGridLayoutBehavior behavior = d as PivotGridLayoutBehavior;
            behavior?.OnLayoutChanged();
        }

        private void OnLayoutChanged()
        {
            if (!string.IsNullOrWhiteSpace(Layout))
            {
                using (MemoryStream stream = new MemoryStream())
                {
                    StreamWriter writer = new StreamWriter(stream);
                    writer.Write(Layout);
                    writer.Flush();

                    stream.Position = 0;

                    AssociatedObject.RestoreLayoutFromStream(stream);
                }
            }
        }
    }
}