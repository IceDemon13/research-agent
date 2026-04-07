using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.AdditionalService
{
    public sealed class AdditionalServiceGroupParameter : EditorParameter
    {
        public AdditionalServiceGroupParameter(int id, int? parentId, string parentName)
            : base(id)
        {
            ParentId = parentId;
            ParentName = parentName;
        }

        public int? ParentId { get; set; }

        public string ParentName { get; set; }
    }
}