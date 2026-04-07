using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.Dialogs
{
    public class GetEmployeeFromUserParameter : EditorParameter
    {
        public GetEmployeeFromUserParameter(int id, int employeeId, string title)
            : base(id)
        {
            EmployeeId = employeeId;
            Title = title;
        }

        public int EmployeeId { get; }

        public string Title { get; }
    }
}
