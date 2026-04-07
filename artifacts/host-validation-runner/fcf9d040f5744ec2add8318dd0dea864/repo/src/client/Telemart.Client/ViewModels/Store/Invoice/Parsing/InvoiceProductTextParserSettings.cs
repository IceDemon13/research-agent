using System;

namespace Telemart.Client.ViewModels.Store.Invoice.Parsing
{
    public readonly struct InvoiceProductTextParserSettings : IEquatable<InvoiceProductTextParserSettings>
    {
        public InvoiceProductTextParserSettings(
            int productIdIndex,
            int productNameIndex,
            int supplierProductIdIndex,
            int supplierProductNameIndex,
            int quantityIndex,
            int priceIndex,
            int pnIndex)
        {
            ProductIdIndex = productIdIndex;
            ProductNameIndex = productNameIndex;
            SupplierProductIdIndex = supplierProductIdIndex;
            SupplierProductNameIndex = supplierProductNameIndex;
            QuantityIndex = quantityIndex;
            PriceIndex = priceIndex;
            PnIndex = pnIndex;
        }

        public int ProductIdIndex { get; }

        public int ProductNameIndex { get; }

        public int SupplierProductIdIndex { get; }

        public int SupplierProductNameIndex { get; }

        public int QuantityIndex { get; }

        public int PriceIndex { get; }

        public int PnIndex { get; }

        public static bool operator ==(InvoiceProductTextParserSettings left, InvoiceProductTextParserSettings right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(InvoiceProductTextParserSettings left, InvoiceProductTextParserSettings right)
        {
            return !(left == right);
        }

        public bool Equals(InvoiceProductTextParserSettings other)
        {
            return ProductIdIndex == other.ProductIdIndex
                   && ProductNameIndex == other.ProductNameIndex
                   && SupplierProductIdIndex == other.SupplierProductIdIndex
                   && SupplierProductNameIndex == other.SupplierProductNameIndex
                   && QuantityIndex == other.QuantityIndex
                   && PriceIndex == other.PriceIndex
                   && PnIndex == other.PnIndex;
        }

        public override bool Equals(object obj)
        {
            return obj is InvoiceProductTextParserSettings other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(ProductIdIndex, ProductNameIndex, SupplierProductIdIndex, SupplierProductNameIndex, QuantityIndex, PriceIndex, PnIndex);
        }
    }
}