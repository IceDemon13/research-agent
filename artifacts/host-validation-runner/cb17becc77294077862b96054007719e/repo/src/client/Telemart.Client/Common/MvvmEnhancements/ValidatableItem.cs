using DevExpress.Mvvm;

namespace Telemart.Client.Common.MvvmEnhancements
{
    public class ValidatableItem : BindableBase
    {
        public ValidatableItem()
        {
            Valid = true;
        }

        public int Id
        {
            get { return GetProperty(() => Id); }
            set { SetProperty(() => Id, value); }
        }

        public string Name
        {
            get { return GetProperty(() => Name); }
            set { SetProperty(() => Name, value); }
        }

        public bool Valid
        {
            get { return GetProperty(() => Valid); }
            set { SetProperty(() => Valid, value); }
        }
    }
}