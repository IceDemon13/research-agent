using DevExpress.Mvvm;

namespace Telemart.Client.ViewModels.Directories.Employee
{
    public class EmployeeOperationViewItem : BindableBase
    {
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

        public string Description
        {
            get { return GetProperty(() => Description); }
            set { SetProperty(() => Description, value); }
        }

        public bool Allow
        {
            get { return GetProperty(() => Allow); }
            set { SetProperty(() => Allow, value); }
        }
    }
}