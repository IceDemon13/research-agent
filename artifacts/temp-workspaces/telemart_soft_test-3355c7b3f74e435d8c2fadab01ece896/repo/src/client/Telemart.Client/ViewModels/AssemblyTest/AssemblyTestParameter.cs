using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.AssemblyTest
{
    public class AssemblyTestParameter : EditorParameter
    {
        public AssemblyTestParameter(int id, int? groupId, string groupName)
            : base(id)
        {
            GroupId = groupId;
            GroupName = groupName;
        }

        public int? GroupId { get; set; }

        public string GroupName { get; set; }
    }
}
