using System;
using System.Windows;
using System.Windows.Media.Imaging;
using DevExpress.Xpf.Core;

namespace Telemart.Client.ViewComponents
{
    public sealed class EditCommentButton : SimpleButton
    {
        public EditCommentButton()
        {
            Width = 26;
            MinWidth = 26;
            ToolTip = "Изменить комментарий";
            Padding = new Thickness(4, 0, 0, 0);
            Glyph = new BitmapImage(new Uri("pack://application:,,,/Telemart.Client;component/Images/Common/comment_edit_16x16.png"));
        }
    }
}
