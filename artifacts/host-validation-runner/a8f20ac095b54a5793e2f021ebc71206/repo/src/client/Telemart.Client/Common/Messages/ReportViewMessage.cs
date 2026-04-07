using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.Common.Messages
{
    public sealed class ReportViewMessage : EditorParameter
    {
        public ReportViewMessage(int id)
            : base(id)
        {
        }

        public int? ParentId { get; set; }
    }
}