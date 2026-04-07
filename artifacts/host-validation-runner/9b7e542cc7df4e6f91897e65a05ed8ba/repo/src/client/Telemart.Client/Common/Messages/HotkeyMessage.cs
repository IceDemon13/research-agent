using System.Collections.Generic;
using System.Windows.Input;

namespace Telemart.Client.Common.Messages
{
    public sealed class HotkeyMessage
    {
        private static readonly Dictionary<string, HotkeyMessageType> HotkeyToMessageTypeMap = new Dictionary<string, HotkeyMessageType>
        {
            ["f5"] = HotkeyMessageType.Refresh,
            ["f6"] = HotkeyMessageType.ShowColumnChooser,
            ["f2"] = HotkeyMessageType.Edit,
            ["insert"] = HotkeyMessageType.Add,
            ["delete"] = HotkeyMessageType.Delete
        };

        public HotkeyMessage(Key key, ModifierKeys modifierKeys)
        {
            Key = key;
            ModifierKeys = modifierKeys;

            HotkeyMessageType hotkeyMessageType;

            if (HotkeyToMessageTypeMap.TryGetValue(Key.ToString().ToLower(), out hotkeyMessageType))
            {
                HotkeyMessageType = hotkeyMessageType;
            }
            else
            {
                HotkeyMessageType = HotkeyMessageType.None;
            }
        }

        public HotkeyMessageType HotkeyMessageType { get; private set; }

        public Key Key { get; }

        public ModifierKeys ModifierKeys { get; }
    }
}
