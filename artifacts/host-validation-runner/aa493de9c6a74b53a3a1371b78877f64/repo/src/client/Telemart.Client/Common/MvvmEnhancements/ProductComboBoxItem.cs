using System;

namespace Telemart.Client.Common.MvvmEnhancements
{
    public struct ProductComboBoxItem : IEquatable<ProductComboBoxItem>, IComparable<ProductComboBoxItem>
    {
        public ProductComboBoxItem(int id, string displayValue, bool keepSerial = false, decimal price = 0)
        {
            Id = id;
            DisplayValue = displayValue;
            KeepSerial = keepSerial;
            Price = price;
        }

        public int Id { get; }

        public string DisplayValue { get; }

        public decimal Price { get; }

        public bool KeepSerial { get; }

        public static bool operator !=(ProductComboBoxItem left, ProductComboBoxItem right)
        {
            return !left.Equals(right);
        }

        public static bool operator ==(ProductComboBoxItem left, ProductComboBoxItem right)
        {
            return left.Equals(right);
        }

        public static bool operator <(ProductComboBoxItem left, ProductComboBoxItem right)
        {
            return left.Id.CompareTo(right.Id) < 0;
        }

        public static bool operator <=(ProductComboBoxItem left, ProductComboBoxItem right)
        {
            return left.Id.CompareTo(right.Id) <= 0;
        }

        public static bool operator >(ProductComboBoxItem left, ProductComboBoxItem right)
        {
            return left.Id.CompareTo(right.Id) > 0;
        }

        public static bool operator >=(ProductComboBoxItem left, ProductComboBoxItem right)
        {
            return left.Id.CompareTo(right.Id) >= 0;
        }

        public override bool Equals(object obj)
        {
            if (obj is null)
            {
                return false;
            }

            return obj is ProductComboBoxItem item && Equals(item);
        }

        public bool Equals(ProductComboBoxItem other)
        {
            return Id == other.Id;
        }

        public override int GetHashCode()
        {
            return Id;
        }

        public int CompareTo(ProductComboBoxItem other)
        {
            return string.Compare(DisplayValue, other.DisplayValue, StringComparison.Ordinal);
        }

        public override string ToString()
        {
            return DisplayValue;
        }
    }
}
