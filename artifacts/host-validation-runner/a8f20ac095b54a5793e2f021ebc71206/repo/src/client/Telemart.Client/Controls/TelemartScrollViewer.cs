using System.Windows.Controls;
using System.Windows.Input;

namespace Telemart.Client.Controls
{
    public class TelemartScrollViewer : ScrollViewer
    {
        protected override void OnMouseWheel(MouseWheelEventArgs e)
        {
            if (e.Delta > 0 && !ContentVerticalOffset.Equals(0))
            {
                base.OnMouseWheel(e);
            }
            else if (e.Delta < 0 && ContentVerticalOffset < ScrollableHeight)
            {
                base.OnMouseWheel(e);
            }
        }
    }
}
