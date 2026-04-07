using System;

namespace Telemart.Client.Common.MvvmEnhancements
{
    public struct ComboBoxItem : IEquatable<ComboBoxItem>, IComparable<ComboBoxItem>
    {
        public ComboBoxItem(int id, string displayValue, bool active = true, string reference = null)
        {
            Id = id;
            DisplayValue = displayValue;
            Active = active;
            Ref = reference;
        }

        public string DisplayValue { get; }

        public int Id { get; }

        public string Ref { get; }

        public bool Active { get; }

        public static bool operator !=(ComboBoxItem left, ComboBoxItem right)
        {
            return !left.Equals(right);
        }

        public static bool operator ==(ComboBoxItem left, ComboBoxItem right)
        {
            return left.Equals(right);
        }

        public static bool operator <(ComboBoxItem left, ComboBoxItem right)
        {
            return left.CompareTo(right) < 0;
        }

        public static bool operator <=(ComboBoxItem left, ComboBoxItem right)
        {
            return left.CompareTo(right) <= 0;
        }

        public static bool operator >(ComboBoxItem left, ComboBoxItem right)
        {
            return left.CompareTo(right) > 0;
        }

        public static bool operator >=(ComboBoxItem left, ComboBoxItem right)
        {
            return left.CompareTo(right) >= 0;
        }

        public override bool Equals(object obj)
        {
            if (obj is null)
            {
                return false;
            }

            return obj is ComboBoxItem item && Equals(item);
        }

        public bool Equals(ComboBoxItem other)
        {
            return Id == other.Id && Ref == other.Ref;
        }

        public override int GetHashCode()
        {
            return Id;
        }

        public int CompareTo(ComboBoxItem other)
        {
            return string.Compare(DisplayValue, other.DisplayValue, StringComparison.Ordinal);
        }

        public override string ToString()
        {
            return DisplayValue;
        }
    }
}