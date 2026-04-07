using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.AdditionalService
{
    public class AdditionalServiceParameter : EditorParameter
    {
        public AdditionalServiceParameter(int id, int groupId, string groupName)
            : base(id)
        {
            GroupName = groupName;
            GroupId = groupId;
        }

        public string GroupName { get; }

        public int GroupId { get; }
    }
}