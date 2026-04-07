using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.CompanyStructure
{
    public sealed class DepartmentViewItem : TelemartEditorViewItemBase
    {
        public DepartmentViewItem()
        {
        }

        public int? ParentDepartmentId
        {
            get { return GetProperty(() => ParentDepartmentId); }
            set { SetProperty(() => ParentDepartmentId, value); }
        }

        public string Name
        {
            get { return GetProperty(() => Name); }
            set { SetProperty(() => Name, value); }
        }

        public int? EmployeeId
        {
            get { return GetProperty(() => EmployeeId); }
            set { SetProperty(() => EmployeeId, value); }
        }

        public bool Active
        {
            get { return GetProperty(() => Active); }
            set { SetProperty(() => Active, value); }
        }

        public int[] Employees
        {
            get { return GetProperty(() => Employees); }
            set { SetProperty(() => Employees, value); }
        }
    }
}