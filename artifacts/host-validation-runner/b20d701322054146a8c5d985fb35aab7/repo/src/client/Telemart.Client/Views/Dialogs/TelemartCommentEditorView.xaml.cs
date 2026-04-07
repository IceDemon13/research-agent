using System;
using System.Windows;
using System.Windows.Threading;

namespace Telemart.Client.Views.Dialogs
{
    /// <summary>
    /// Interaction logic for ServiceRequestEditCommentView.xaml
    /// </summary>
    public partial class TelemartCommentEditorView
    {
        public TelemartCommentEditorView()
        {
            InitializeComponent();
        }

        private void TelemartCommentEditorViewOnLoaded(object sender, RoutedEventArgs e)
        {
            Dispatcher.BeginInvoke(DispatcherPriority.ContextIdle, new Action(() => CommentTextEdit.Focus()));
        }
    }
}
