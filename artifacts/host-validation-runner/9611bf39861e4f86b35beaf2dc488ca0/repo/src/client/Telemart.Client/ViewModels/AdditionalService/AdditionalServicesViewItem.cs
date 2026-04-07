using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.AdditionalService
{
    public class AdditionalServicesViewItem : TelemartViewItemBase
    {
        public int Id
        {
            get { return GetProperty(() => Id); }
            set { SetProperty(() => Id, value); }
        }

        public int GroupId
        {
            get { return GetProperty(() => GroupId); }
            set { SetProperty(() => GroupId, value); }
        }

        public string GroupName
        {
            get { return GetProperty(() => GroupName); }
            set { SetProperty(() => GroupName, value); }
        }

        public string Name
        {
            get { return GetProperty(() => Name); }
            set { SetProperty(() => Name, value); }
        }

        public int Position
        {
            get { return GetProperty(() => Position); }
            set { SetProperty(() => Position, value); }
        }

        public int Priority
        {
            get { return GetProperty(() => Priority); }
            set { SetProperty(() => Priority, value); }
        }

        public int PriorityTypeId
        {
            get { return GetProperty(() => PriorityTypeId); }
            set { SetProperty(() => PriorityTypeId, value); }
        }

        public bool Active
        {
            get { return GetProperty(() => Active); }
            set { SetProperty(() => Active, value); }
        }

        public int? EmployeeLockId
        {
            get { return GetProperty(() => EmployeeLockId); }
            set { SetProperty(() => EmployeeLockId, value); }
        }

        public string EmployeeLockName
        {
            get { return GetProperty(() => EmployeeLockName); }
            set { SetProperty(() => EmployeeLockName, value); }
        }
    }
}