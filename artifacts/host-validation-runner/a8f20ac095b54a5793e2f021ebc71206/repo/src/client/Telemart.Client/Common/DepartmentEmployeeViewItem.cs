using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.Common
{
    public class DepartmentEmployeeViewItem : TelemartViewItemBase
    {
        public int DepartmentId
        {
            get { return GetProperty(() => DepartmentId); }
            init { SetProperty(() => DepartmentId, value); }
        }

        public int? ParentDepartmentId
        {
            get { return GetProperty(() => ParentDepartmentId); }
            init { SetProperty(() => ParentDepartmentId, value); }
        }

        public int? EmployeeId
        {
            get { return GetProperty(() => EmployeeId); }
            init { SetProperty(() => EmployeeId, value); }
        }

        public string DisplayName
        {
            get { return GetProperty(() => DisplayName); }
            init { SetProperty(() => DisplayName, value); }
        }
    }
}