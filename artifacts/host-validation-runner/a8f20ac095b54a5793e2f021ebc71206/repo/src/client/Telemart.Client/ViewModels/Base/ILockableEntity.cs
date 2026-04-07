namespace Telemart.Client.ViewModels.Base
{
    public interface ILockableEntity
    {
        int Id { get; set; }

        int? EmployeeLockId { get; }

        string EmployeeLockName { get; }
    }
}