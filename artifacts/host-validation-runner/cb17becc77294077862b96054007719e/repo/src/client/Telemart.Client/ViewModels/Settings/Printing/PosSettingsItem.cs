using DevExpress.Mvvm;
using Telemart.Client.Common.Settings.Equipment;

namespace Telemart.Client.ViewModels.Settings.Printing
{
    public sealed class PosSettingsItem : BindableBase
    {
        public int Id
        {
            get { return GetProperty(() => Id); }
            set { SetProperty(() => Id, value); }
        }

        public int PosTypeId
        {
            get { return GetProperty(() => PosTypeId); }
            set { SetProperty(() => PosTypeId, value); }
        }

        public string IpAddress
        {
            get { return GetProperty(() => IpAddress); }
            set { SetProperty(() => IpAddress, value); }
        }

        public string Merchant
        {
            get { return GetProperty(() => Merchant); }
            set { SetProperty(() => Merchant, value); }
        }

        public int PosCashboxId
        {
            get { return GetProperty(() => PosCashboxId); }
            set { SetProperty(() => PosCashboxId, value); }
        }

        public int? PosLegalEntityId
        {
            get { return GetProperty(() => PosLegalEntityId); }
            set { SetProperty(() => PosLegalEntityId, value); }
        }

        public string MacAddress
        {
            get { return GetProperty(() => MacAddress); }
            set { SetProperty(() => MacAddress, value); }
        }
    }
}