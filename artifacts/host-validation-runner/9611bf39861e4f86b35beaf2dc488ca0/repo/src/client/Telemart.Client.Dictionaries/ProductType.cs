namespace Telemart.Client.Dictionaries
{
    public class ProductType : DictionaryItem
    {
        public const int ProductId = 1;
        public const int CertificateId = 2;
        public const int ServiceCertificateId = 3;
        public const int ServiceId = 4;
        public const int AssemblyServiceId = 5;
        public const int AccessoryId = 6;
        public const int ServiceAdditionalServiceId = 7;
        public const int ProductServiceId = 8;
        public const int TradeInId = 9;
        public const int AssembledComputerRuleId = 10;
        public const int ProductInAssemblyNotCollectId = 11;
        public const int ProductInAssemblyCollectId = 12;
        public const int ProductInAssemblyCollectForconfigurationId = 13;
        public const int ProductWithServiceId = 14;
        public const int ProductAdditionalServiceConsumableId = 15;
        public const int GuestProductId = 16;
        public const int RefId = 17;
        public const int DiscountId = 18;
        public const int SecondHandId = 19;

        public ProductType(int id, string name, bool isVirtual, bool allowedInAssemblyConfiguration)
            : base(id, name, true)
        {
            IsVirtual = isVirtual;
            AllowedInAssembledComputerRule = allowedInAssemblyConfiguration;
        }

        public bool IsVirtual { get; }

        public bool AllowedInAssembledComputerRule { get; }

        public bool IsService
        {
            get =>
                Id == ServiceCertificateId ||
                Id == ServiceId;
        }

        public static bool IsAdditionalServiceProductType(int typeId)
        {
            return typeId == ServiceId || typeId == ServiceCertificateId || typeId == CertificateId;
        }

        public static bool IsAccessoryAdditionalServiceProductType(int typeId)
        {
            return typeId == AccessoryId;
        }
    }
}