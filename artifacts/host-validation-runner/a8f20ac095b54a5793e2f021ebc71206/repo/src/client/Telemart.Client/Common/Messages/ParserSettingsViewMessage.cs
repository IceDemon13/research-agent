using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.Common.Messages
{
    public sealed class ParserSettingsViewMessage : EditorParameter
    {
        public ParserSettingsViewMessage(int id)
            : base(id)
        {
        }
    }
}