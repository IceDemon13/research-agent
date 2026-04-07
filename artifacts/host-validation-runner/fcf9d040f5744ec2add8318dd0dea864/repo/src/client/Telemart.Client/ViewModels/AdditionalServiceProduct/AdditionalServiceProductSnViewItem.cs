using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.TransferObjects;
using Telemart.Client.ViewModels.Base;
using Telemart.Common.Localization;

namespace Telemart.Client.ViewModels.AdditionalServiceProduct
{
    public class AdditionalServiceProductSnViewItem : TelemartViewItemBase, ILocalіzableEntity
    {
        public int ProductId
        {
            get { return GetProperty(() => ProductId); }
            set { SetProperty(() => ProductId, value); }
        }

        public int ProductTypeId
        {
            get { return GetProperty(() => ProductTypeId); }
            set { SetProperty(() => ProductTypeId, value, () => RaisePropertyChanged(nameof(IsGuestProduct))); }
        }

        public int? ConsumableId
        {
            get { return GetProperty(() => ConsumableId); }
            set { SetProperty(() => ConsumableId, value); }
        }

        public string DisplayName => this.GetLocalName(LocalizableNameType.Ukr);

        public string SerialNumber
        {
            get { return GetProperty(() => SerialNumber); }
            set { SetProperty(() => SerialNumber, value); }
        }

        public string GroupString
        {
            get { return GetProperty(() => GroupString); }
            set { SetProperty(() => GroupString, value); }
        }

        public bool KeepSerial
        {
            get { return GetProperty(() => KeepSerial); }
            set { SetProperty(() => KeepSerial, value, () => KeepSerialOverriden = value); }
        }

        public bool KeepSerialOverriden
        {
            get { return GetProperty(() => KeepSerialOverriden); }
            set { SetProperty(() => KeepSerialOverriden, value); }
        }

        public bool Scanned
        {
            get { return GetProperty(() => Scanned); }
            set { SetProperty(() => Scanned, value); }
        }

        public GuestProductDto GuestProduct
        {
            get { return GetProperty(() => GuestProduct); }
            set { SetProperty(() => GuestProduct, value); }
        }

        public bool IsGuestProduct => ProductTypeId == ProductType.GuestProductId;

        public string Name { get; set; }

        public string NameUkr { get; set; }

        public string NameEn { get; set; }
    }
}