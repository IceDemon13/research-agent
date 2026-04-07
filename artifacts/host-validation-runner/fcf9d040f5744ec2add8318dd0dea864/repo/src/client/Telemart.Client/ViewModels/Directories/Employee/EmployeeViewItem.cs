using System;
using DevExpress.Mvvm.DataAnnotations;
using DevExpress.Mvvm.POCO;
using Telemart.Client.Dictionaries;

namespace Telemart.Client.ViewModels.Directories.Employee
{
    [POCOViewModel]
    public class EmployeeViewItem
    {
        protected EmployeeViewItem()
        {
        }

        public virtual int Id { get; set; }

        public virtual int? BitrixId { get; set; }

        public virtual string Name { get; set; }

        public virtual string ShortName { get; set; }

        public virtual string Login { get; set; }

        public virtual int? CityId { get; set; }

        public virtual int DepartmentId { get; set; }

        public virtual string Position { get; set; }

        public virtual string Phone1 { get; set; }

        public virtual string Phone2 { get; set; }

        public virtual string Email { get; set; }

        public virtual string Skype { get; set; }

        public virtual string Telegram { get; set; }

        public virtual bool Active { get; set; }

        public virtual Subdivision Subdivision { get; set; }

        public virtual DateTime ModifiedOn { get; set; }

        public virtual int ModifiedBy { get; set; }

        public virtual DateTime CreatedOn { get; set; }

        public virtual int CreatedBy { get; set; }

        public virtual int? EmployeeLockId { get; set; }

        public virtual string EmployeeLockName { get; set; }

        public static EmployeeViewItem Create()
        {
            return ViewModelSource<EmployeeViewItem>.Create();
        }
    }
}