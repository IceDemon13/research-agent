namespace Telemart.Client.ViewModels.Directories.Employee
{
    public sealed class EmployeeCopyParameter
    {
        public EmployeeCopyParameter(int employeeId)
        {
            EmployeeId = employeeId;
        }

        public int EmployeeId { get; }
    }
}