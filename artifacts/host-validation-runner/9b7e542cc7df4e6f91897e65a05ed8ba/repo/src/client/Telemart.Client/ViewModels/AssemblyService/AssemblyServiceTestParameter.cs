using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.AssemblyService
{
    public class AssemblyServiceTestParameter : EditorParameter
    {
        public AssemblyServiceTestParameter(int id, bool isFormEditable)
            : base(id)
        {
            IsFormEditable = isFormEditable;
        }

        public bool IsFormEditable { get; }
    }
}
