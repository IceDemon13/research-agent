using Telemart.Client.Dictionaries;
using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.CompanyStructure
{
    public class EmployeeDepartmentViewItem : TelemartViewItemBase
    {
        public EmployeeDepartmentViewItem(int id, string name, string position, string phone, DepartmentEmployeeType departmentEmployeeType)
        {
            Id = id;
            Name = name;
            Position = position;
            Phone = phone;
            DepartmentEmployeeType = departmentEmployeeType;
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

        public string Position
        {
            get { return GetProperty(() => Position); }
            set { SetProperty(() => Position, value); }
        }

        public string Phone
        {
            get { return GetProperty(() => Phone); }
            set { SetProperty(() => Phone, value); }
        }

        public DepartmentEmployeeType DepartmentEmployeeType
        {
            get { return GetProperty(() => DepartmentEmployeeType); }
            set { SetProperty(() => DepartmentEmployeeType, value); }
        }
    }
}