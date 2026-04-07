using Telemart.Client.Common.Messages;

namespace Telemart.Client.ViewModels
{
    public interface ISupportHotkeys
    {
        bool HandleHotkey(HotkeyMessage hotkeyMessage);
    }
}
