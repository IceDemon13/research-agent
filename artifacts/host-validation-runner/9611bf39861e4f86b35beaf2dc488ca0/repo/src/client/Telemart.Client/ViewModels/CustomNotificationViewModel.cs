using System.Windows.Media;
using DevExpress.Mvvm.DataAnnotations;
using DevExpress.Mvvm.POCO;

namespace Telemart.Client.ViewModels
{
    [POCOViewModel]
    public class CustomNotificationViewModel
    {
        protected CustomNotificationViewModel()
        {
        }

        public virtual string Caption { get; set; }

        public virtual string Content { get; set; }

        public virtual string DateTime { get; set; }

        public virtual ImageSource Image { get; set; }

        public static CustomNotificationViewModel Create(string caption, string content, ImageSource image)
        {
            CustomNotificationViewModel viewModel = ViewModelSource<CustomNotificationViewModel>.Create();

            viewModel.Caption = caption;
            viewModel.Content = content;
            viewModel.Image = image;
            viewModel.DateTime = System.DateTime.Now.ToLongTimeString();

            return viewModel;
        }
    }
}