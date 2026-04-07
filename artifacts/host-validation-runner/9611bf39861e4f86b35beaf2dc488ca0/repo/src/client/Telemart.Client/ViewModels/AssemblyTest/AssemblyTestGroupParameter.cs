using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.AssemblyTest
{
    public sealed class AssemblyTestGroupParameter : EditorParameter
    {
        public AssemblyTestGroupParameter(int id, int? parentId, string parentName)
            : base(id)
        {
            ParentId = parentId;
            ParentName = parentName;
        }

        public int? ParentId { get; set; }

        public string ParentName { get; set; }
    }
}