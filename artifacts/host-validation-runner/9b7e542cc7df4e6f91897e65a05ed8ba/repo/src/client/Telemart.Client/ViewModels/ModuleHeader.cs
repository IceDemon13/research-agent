using DevExpress.Mvvm;

namespace Telemart.Client.ViewModels
{
    public class ModuleHeader : BindableBase
    {
        public ModuleHeader(string image, string title, bool hasImage)
        {
            Image = image;
            Title = title;
            HasImage = hasImage;
        }

        public string Image { get; }

        public bool HasImage { get; }

        public string Title
        {
            get { return GetProperty(() => Title); }
            private set { SetProperty(() => Title, value); }
        }

        public void SetTitle(string title)
        {
            Title = title;
        }

        public override string ToString()
        {
            return Title;
        }
    }
}
