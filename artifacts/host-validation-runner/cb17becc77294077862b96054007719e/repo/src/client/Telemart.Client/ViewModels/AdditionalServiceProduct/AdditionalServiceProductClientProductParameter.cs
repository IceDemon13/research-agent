namespace Telemart.Client.ViewModels.AdditionalServiceProduct
{
    public sealed class AdditionalServiceProductClientProductParameter
    {
        public AdditionalServiceProductClientProductParameter(
            string product,
            string serialNumber,
            string description,
            bool isLockedByCurrentUser,
            int? additionalServiceWarehouseId,
            bool keepProduct)
        {
            KeepProduct = keepProduct;
            Product = product;
            AdditionalServiceWarehouseId = additionalServiceWarehouseId;
            SerialNumber = serialNumber;
            Description = description;
            IsLockedByCurrentEmployee = isLockedByCurrentUser;
        }

        public string Product { get; }

        public string SerialNumber { get; }

        public string Description { get; }

        public bool IsLockedByCurrentEmployee { get; }

        public int? AdditionalServiceWarehouseId { get; }

        public bool KeepProduct { get; }
    }
}