using System.Linq;
using System.Windows;

namespace Telemart.Client.Common
{
    public static class PhoneCopingToClipboardHelper
    {
        public static void Handle(object sender, DataObjectCopyingEventArgs e)
        {
            e.CancelCommand();

            string rawPhoneValue = e.DataObject.GetData(DataFormats.UnicodeText) as string;

            if (string.IsNullOrEmpty(rawPhoneValue))
            {
                return;
            }

            string clearPhoneValue = new string(rawPhoneValue.ToCharArray().Where(x => "0123456789".Contains(x)).ToArray());

            Clipboard.SetDataObject(clearPhoneValue);
        }
    }
}
