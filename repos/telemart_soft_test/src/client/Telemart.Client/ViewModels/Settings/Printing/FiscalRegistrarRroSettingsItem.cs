using System.Collections.ObjectModel;
using DevExpress.Mvvm;

namespace Telemart.Client.ViewModels.Settings.Printing
{
    public sealed class FiscalRegistrarRroSettingsItem : BindableBase
    {
        public int Id
        {
            get { return GetProperty(() => Id); }
            set { SetProperty(() => Id, value); }
        }

        public string UniqueDeviceId
        {
            get { return GetProperty(() => UniqueDeviceId); }
            set { SetProperty(() => UniqueDeviceId, value); }
        }

        public int FiscalConnectionTypeId
        {
            get { return GetProperty(() => FiscalConnectionTypeId); }
            set { SetProperty(() => FiscalConnectionTypeId, value); }
        }

        public int FiscalRegistrarCashboxId
        {
            get { return GetProperty(() => FiscalRegistrarCashboxId); }
            set { SetProperty(() => FiscalRegistrarCashboxId, value); }
        }

        public int? FiscalRegistrarLegalEntityId
        {
            get { return GetProperty(() => FiscalRegistrarLegalEntityId); }
            set { SetProperty(() => FiscalRegistrarLegalEntityId, value); }
        }

        public string FiscalRegistrarIp
        {
            get { return GetProperty(() => FiscalRegistrarIp); }
            set { SetProperty(() => FiscalRegistrarIp, value); }
        }

        public string MacAddress
        {
            get { return GetProperty(() => MacAddress); }
            set { SetProperty(() => MacAddress, value); }
        }

        public string FiscalRegistrarUser
        {
            get { return GetProperty(() => FiscalRegistrarUser); }
            set { SetProperty(() => FiscalRegistrarUser, value); }
        }

        public string FiscalRegistrarPassword
        {
            get { return GetProperty(() => FiscalRegistrarPassword); }
            set { SetProperty(() => FiscalRegistrarPassword, value); }
        }

        public bool Active
        {
            get { return GetProperty(() => Active); }
            set { SetProperty(() => Active, value); }
        }

        public bool Selected
        {
            get { return GetProperty(() => Selected); }
            set { SetProperty(() => Selected, value); }
        }

        public bool SessionIsOpen
        {
            get { return GetProperty(() => SessionIsOpen); }
            set { SetProperty(() => SessionIsOpen, value, () => RaisePropertyChanged(nameof(ToolTipText))); }
        }

        public string ToolTipText => SessionIsOpen
            ? "Смена открыта"
            : "Смена закрыта";
    }
}