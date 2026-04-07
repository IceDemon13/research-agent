using DevExpress.Mvvm;
using Telemart.Client.Common.MvvmEnhancements;

namespace Telemart.Client.ViewModels.Directories.Employee
{
    public class EmployeeAccountViewItem : BindableBase
    {
        public int Id
        {
            get { return GetProperty(() => Id); }
            set { SetProperty(() => Id, value); }
        }

        public int AccountId
        {
            get { return GetProperty(() => AccountId); }
            set { SetProperty(() => AccountId, value); }
        }

        public ComboBoxItem Account
        {
            get { return GetProperty(() => Account); }
            set { SetProperty(() => Account, value); }
        }

        public string Login
        {
            get { return GetProperty(() => Login); }
            set { SetProperty(() => Login, value); }
        }

        public string Password
        {
            get { return GetProperty(() => Password); }
            set { SetProperty(() => Password, value, () => RaisePropertyChanged(nameof(DisplayPassword))); }
        }

        public bool Active
        {
            get { return GetProperty(() => Active); }
            set { SetProperty(() => Active, value); }
        }

        public string DisplayPassword => Password?.Length > 0 ? new string('*', Password.Length) : string.Empty;
    }
}
