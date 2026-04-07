using System.Windows.Input;
using DevExpress.Xpf.Core;

namespace Telemart.Client.Views
{
    /// <summary>
    /// Interaction logic for WorkspaceView.xaml
    /// </summary>
    public partial class WorkspaceView
    {
        public WorkspaceView()
        {
            InitializeComponent();
        }

        private void DxTabControlKeyDown(object sender, KeyEventArgs e)
        {
            if (!(sender is DXTabControl tabControl))
            {
                return;
            }

            if (Keyboard.IsKeyDown(Key.LeftCtrl))
            {
                if (Keyboard.IsKeyDown(Key.PageDown))
                {
                    if (tabControl.CanSelectNext())
                    {
                        tabControl.SelectNext();
                    }
                    else
                    {
                        tabControl.SelectedIndex = 0;
                    }
                }
                else if (Keyboard.IsKeyDown(Key.PageUp))
                {
                    if (tabControl.CanSelectPrev())
                    {
                        tabControl.SelectPrev();
                    }
                    else
                    {
                        tabControl.SelectedIndex = tabControl.Items.Count - 1;
                    }
                }
            }
        }
    }
}
