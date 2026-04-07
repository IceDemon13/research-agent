using DevExpress.Mvvm;
using Telemart.Client.Dictionaries;

namespace Telemart.Client.ViewModels.Store.Order.AutoSource
{
    public sealed class OrderProductSourceSettingItem : BindableBase
    {
        public int? CarryId
        {
            get { return GetProperty(() => CarryId); }
            set { SetProperty(() => CarryId, value); }
        }

        public int SourceId
        {
            get { return GetProperty(() => SourceId); }
            set { SetProperty(() => SourceId, value); }
        }

        public int? WarehouseTypeId
        {
            get { return GetProperty(() => WarehouseTypeId); }
            set { SetProperty(() => WarehouseTypeId, value); }
        }

        public string FullNameSource
        {
            get { return GetProperty(() => FullNameSource); }
            set { SetProperty(() => FullNameSource, value); }
        }

        public CarryType CarryType
        {
            get { return GetProperty(() => CarryType); }
            set { SetProperty(() => CarryType, value); }
        }

        public int ProductType
        {
            get { return GetProperty(() => ProductType); }
            set { SetProperty(() => ProductType, value); }
        }

        public int CertificateType
        {
            get { return GetProperty(() => CertificateType); }
            set { SetProperty(() => CertificateType, value); }
        }

        public int ServiceCertificateType
        {
            get { return GetProperty(() => ServiceCertificateType); }
            set { SetProperty(() => ServiceCertificateType, value); }
        }

        public int AssemblyServiceType
        {
            get { return GetProperty(() => AssemblyServiceType); }
            set { SetProperty(() => AssemblyServiceType, value); }
        }

        public int AccessoryType
        {
            get { return GetProperty(() => AccessoryType); }
            set { SetProperty(() => AccessoryType, value); }
        }

        public int AssembledComputerRuleType
        {
            get { return GetProperty(() => AssembledComputerRuleType); }
            set { SetProperty(() => AssembledComputerRuleType, value); }
        }

        public int ProductInAssemblyNotCollectType
        {
            get { return GetProperty(() => ProductInAssemblyNotCollectType); }
            set { SetProperty(() => ProductInAssemblyNotCollectType, value); }
        }

        public int ProductInAssemblyCollectType
        {
            get { return GetProperty(() => ProductInAssemblyCollectType); }
            set { SetProperty(() => ProductInAssemblyCollectType, value); }
        }

        public int ProductInAssemblyCollectForconfigurationType
        {
            get { return GetProperty(() => ProductInAssemblyCollectForconfigurationType); }
            set { SetProperty(() => ProductInAssemblyCollectForconfigurationType, value); }
        }

        public int ProductWithServiceType
        {
            get { return GetProperty(() => ProductWithServiceType); }
            set { SetProperty(() => ProductWithServiceType, value); }
        }

        public int TradeInType
        {
            get { return GetProperty(() => TradeInType); }
            set { SetProperty(() => TradeInType, value); }
        }

        public int DiscountType
        {
            get { return GetProperty(() => DiscountType); }
            set { SetProperty(() => DiscountType, value); }
        }

        public int RefType
        {
            get { return GetProperty(() => RefType); }
            set { SetProperty(() => RefType, value); }
        }

        public int SecondHandType
        {
            get { return GetProperty(() => SecondHandType); }
            set { SetProperty(() => SecondHandType, value); }
        }

        public int AdditionalServiceConsumableType
        {
            get { return GetProperty(() => AdditionalServiceConsumableType); }
            set { SetProperty(() => AdditionalServiceConsumableType, value); }
        }

        public int ClientProductType
        {
            get { return GetProperty(() => ClientProductType); }
            set { SetProperty(() => ClientProductType, value); }
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }

            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            if (obj.GetType() != GetType())
            {
                return false;
            }

            return Equals((OrderProductSourceSettingItem)obj);
        }

        public bool Equals(OrderProductSourceSettingItem other)
        {
            return other != null
                   && CarryId == other.CarryId
                   && SourceId == other.SourceId
                   && WarehouseTypeId == other.WarehouseTypeId
                   && ProductType == other.ProductType
                   && CertificateType == other.CertificateType
                   && ServiceCertificateType == other.ServiceCertificateType
                   && AssemblyServiceType == other.AssemblyServiceType
                   && AccessoryType == other.AccessoryType
                   && AssembledComputerRuleType == other.AssembledComputerRuleType
                   && ProductInAssemblyNotCollectType == other.ProductInAssemblyNotCollectType
                   && ProductInAssemblyCollectType == other.ProductInAssemblyCollectType
                   && ProductInAssemblyCollectForconfigurationType == other.ProductInAssemblyCollectForconfigurationType
                   && ProductWithServiceType == other.ProductWithServiceType
                   && TradeInType == other.TradeInType
                   && RefType == other.RefType
                   && DiscountType == other.DiscountType
                   && SecondHandType == other.SecondHandType
                   && AdditionalServiceConsumableType == other.AdditionalServiceConsumableType;
        }
    }
}