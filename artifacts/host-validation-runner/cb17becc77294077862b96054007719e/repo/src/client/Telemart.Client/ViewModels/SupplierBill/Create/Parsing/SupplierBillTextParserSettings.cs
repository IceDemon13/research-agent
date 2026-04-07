using System;
using System.Diagnostics.CodeAnalysis;

namespace Telemart.Client.ViewModels.SupplierBill.Create.Parsing
{
    public struct SupplierBillTextParserSettings
    {
        public SupplierBillTextParserSettings(
            int nameIndex,
            int codeIndex,
            int quantityIndex,
            int priceIndex,
            int tnvedIndex,
            bool priceWithTax)
        {
            NameIndex = nameIndex;
            CodeIndex = codeIndex;
            QuantityIndex = quantityIndex;
            PriceIndex = priceIndex;
            TnvedIndex = tnvedIndex;
            PriceWithTax = priceWithTax;
        }

        public int NameIndex { get; }

        public int CodeIndex { get; }

        public int QuantityIndex { get; }

        public int PriceIndex { get; }

        public int TnvedIndex { get; }

        public bool PriceWithTax { get; }

        public static bool operator ==(SupplierBillTextParserSettings left, SupplierBillTextParserSettings right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(SupplierBillTextParserSettings left, SupplierBillTextParserSettings right)
        {
            return !(left == right);
        }

        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}