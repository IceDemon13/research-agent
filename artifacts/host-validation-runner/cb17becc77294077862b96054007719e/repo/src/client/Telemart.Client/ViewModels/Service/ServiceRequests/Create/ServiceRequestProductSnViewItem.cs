using System.Collections.ObjectModel;
using System.Linq;
using DevExpress.Mvvm.DataAnnotations;
using Telemart.Client.Dictionaries;
using Telemart.Client.Properties;
using Telemart.Client.TransferObjects;
using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.Service.ServiceRequests.Create
{
    public class ServiceRequestProductSnViewItem : TelemartViewItemBase
    {
        public int ProductId
        {
            get { return GetProperty(() => ProductId); }
            set { SetProperty(() => ProductId, value); }
        }

        public OrderPromoCodeDto OrderPromoCode
        {
            get { return GetProperty(() => OrderPromoCode); }
            set { SetProperty(() => OrderPromoCode, value, () => RaisePropertyChanged(nameof(BundleProduct))); }
        }

        public string Name
        {
            get { return GetProperty(() => Name); }
            set { SetProperty(() => Name, value); }
        }

        public string SerialNumber
        {
            get { return GetProperty(() => SerialNumber); }
            set { SetProperty(() => SerialNumber, value); }
        }

        public bool WarrantyRemoved
        {
            get { return GetProperty(() => WarrantyRemoved); }
            set { SetProperty(() => WarrantyRemoved, value); }
        }

        public bool AddedForBundleMoneyReturn
        {
            get { return GetProperty(() => AddedForBundleMoneyReturn); }
            set { SetProperty(() => AddedForBundleMoneyReturn, value); }
        }

        public string Defect
        {
            get { return GetProperty(() => Defect); }
            set { SetProperty(() => Defect, value); }
        }

        public int? ChangeOnProductId
        {
            get { return GetProperty(() => ChangeOnProductId); }
            set { SetProperty(() => ChangeOnProductId, value); }
        }

        public string ChangeOnProductName
        {
            get { return GetProperty(() => ChangeOnProductName); }
            set { SetProperty(() => ChangeOnProductName, value); }
        }

        public ServiceRequestRequirement ClientRequirement
        {
            get { return GetProperty(() => ClientRequirement); }
            set { SetProperty(() => ClientRequirement, value, () => RaisePropertyChanged(nameof(Defect))); }
        }

        public ReadOnlyObservableCollection<ServiceRequestSerialNumberViewItem> SerialNumbers
        {
            get { return GetProperty(() => SerialNumbers); }
            set { SetProperty(() => SerialNumbers, value, () => RaisePropertyChanged(nameof(AnySerialNumbers))); }
        }

        public bool AnySerialNumbers => SerialNumbers?.Any() == true;

        public bool BundleProduct => OrderPromoCode != null && OrderPromoCode.PromoCodeTypeId == PromoCodeType.Bundle.Id;

        public static void BuildMetadata(MetadataBuilder<ServiceRequestProductSnViewItem> builder)
        {
            builder.Property(x => x.Defect)
                .MatchesInstanceRule((x, y) => y.ClientRequirement == ServiceRequestRequirement.TradeIn || x != null, () => Resources.RequiredErrorMessage);

            builder.Property(x => x.ChangeOnProductName)
                .MatchesInstanceRule((x, y) => y.ClientRequirement == ServiceRequestRequirement.Change, () => Resources.RequiredErrorMessage);
        }
    }
}
