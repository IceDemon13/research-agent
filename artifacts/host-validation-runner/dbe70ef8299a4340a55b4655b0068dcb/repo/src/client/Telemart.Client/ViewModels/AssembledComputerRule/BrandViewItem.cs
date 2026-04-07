using System;
using Telemart.Client.Common.MvvmEnhancements;

namespace Telemart.Client.ViewModels.AssembledComputerRule
{
    public struct BrandViewItem : IEquatable<BrandViewItem>, IComparable<BrandViewItem>
    {
        public BrandViewItem(int id, string displayValue, string brandName, string prefixRu, string prefixUkr, string prefixEn)
        {
            Id = id;
            DisplayValue = displayValue;
            Prefix = prefixRu;
            PrefixUkr = prefixUkr;
            PrefixEn = prefixEn;
            Name = brandName;
        }

        public string DisplayValue { get; }

        public string Name { get; }

        public int Id { get; }

        public string Prefix { get; }

        public string PrefixUkr { get; }

        public string PrefixEn { get; }

        public static bool operator !=(BrandViewItem left, BrandViewItem right)
        {
            return !left.Equals(right);
        }

        public static bool operator ==(BrandViewItem left, BrandViewItem right)
        {
            return left.Equals(right);
        }

        public static bool operator <(BrandViewItem left, BrandViewItem right)
        {
            return left.CompareTo(right) < 0;
        }

        public static bool operator <=(BrandViewItem left, BrandViewItem right)
        {
            return left.CompareTo(right) <= 0;
        }

        public static bool operator >(BrandViewItem left, BrandViewItem right)
        {
            return left.CompareTo(right) > 0;
        }

        public static bool operator >=(BrandViewItem left, BrandViewItem right)
        {
            return left.CompareTo(right) >= 0;
        }

        public override bool Equals(object obj)
        {
            if (obj is null)
            {
                return false;
            }

            return obj is BrandViewItem item && Equals(item);
        }

        public bool Equals(BrandViewItem other)
        {
            return Id == other.Id;
        }

        public override int GetHashCode()
        {
            return Id;
        }

        public int CompareTo(BrandViewItem other)
        {
            return string.Compare(DisplayValue, other.DisplayValue, StringComparison.Ordinal);
        }

        public override string ToString()
        {
            return DisplayValue;
        }
    }
}