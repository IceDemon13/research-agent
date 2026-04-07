using Telemart.Client.FiscalRegistrar.Abstraction.TransferObjects;

namespace Telemart.Client.ViewModels.Common
{
    public sealed class ChoosePrintRroCheckParameter
    {
        public ChoosePrintRroCheckParameter(string email, string phone, bool emailSetting, bool phoneSetting, bool printerSetting)
        {
            Email = email;
            Phone = phone;
            EmailSetting = emailSetting;
            PhoneSetting = phoneSetting;
            PrinterSetting = printerSetting;
        }

        public string Email { get; }

        public string Phone { get; }

        public bool EmailSetting { get;  }

        public bool PhoneSetting { get; }

        public bool PrinterSetting { get; }
    }
}