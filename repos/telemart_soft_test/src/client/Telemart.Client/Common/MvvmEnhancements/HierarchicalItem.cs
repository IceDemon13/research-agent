using System;

namespace Telemart.Client.Common.MvvmEnhancements
{
    public struct HierarchicalItem : IComparable, IComparable<HierarchicalItem>, IEquatable<HierarchicalItem>
    {
        public HierarchicalItem(int id, string displayValue, int? parentId = null, bool active = true)
        {
            Id = id;
            ParentId = parentId;
            DisplayValue = displayValue;
            Active = active;
        }

        public string DisplayValue { get; }

        public int Id { get; }

        public int? ParentId { get; }

        public bool Active { get; }

        public static bool operator !=(HierarchicalItem left, HierarchicalItem right)
        {
            return !left.Equals(right);
        }

        public static bool operator ==(HierarchicalItem left, HierarchicalItem right)
        {
            return left.Equals(right);
        }

        public static bool operator <(HierarchicalItem left, HierarchicalItem right)
        {
            return left.CompareTo(right) < 0;
        }

        public static bool operator <=(HierarchicalItem left, HierarchicalItem right)
        {
            return left.CompareTo(right) <= 0;
        }

        public static bool operator >(HierarchicalItem left, HierarchicalItem right)
        {
            return left.CompareTo(right) > 0;
        }

        public static bool operator >=(HierarchicalItem left, HierarchicalItem right)
        {
            return left.CompareTo(right) >= 0;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }

            return obj is HierarchicalItem item && Equals(item);
        }

        public bool Equals(HierarchicalItem other)
        {
            return Id == other.Id;
        }

        public override int GetHashCode()
        {
            return Id;
        }

        public int CompareTo(HierarchicalItem other)
        {
            return string.Compare(DisplayValue, other.DisplayValue, StringComparison.Ordinal);
        }

        public override string ToString()
        {
            return DisplayValue;
        }

        public int CompareTo(object obj)
        {
            if (obj is HierarchicalItem item)
            {
                return CompareTo(item);
            }

            return 1;
        }
    }
}
