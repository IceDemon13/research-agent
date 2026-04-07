using System;
using System.Windows.Media;

namespace Telemart.Client.ViewModels.Validation
{
    public sealed class ProductComparisonResult : IComparable, IComparable<ProductComparisonResult>
    {
        private readonly int deviationQuantity;

        public ProductComparisonResult(string productName, int expectedQuantity, int quantityReal)
        {
            ProductName = productName;
            ExpectedQuantity = expectedQuantity;
            QuantityReal = quantityReal;

            deviationQuantity = QuantityReal - ExpectedQuantity;

            Deviation = deviationQuantity > 0
                ? $"+{deviationQuantity}"
                : deviationQuantity.ToString();

            Color = deviationQuantity > 0
                ? Brushes.Green
                : deviationQuantity < 0 ? Brushes.Red : Brushes.Black;
        }

        public ProductComparisonResult(string productName, int expectedQuantity, int quantityReal, int? productId)
        : this(productName, expectedQuantity, quantityReal)
        {
            ProductId = productId;
        }

        public string ProductName { get; }

        public int ExpectedQuantity { get; set; }

        public int QuantityReal { get; set; }

        public int DeviationQuantity => deviationQuantity;

        public string Deviation { get; }

        public int? ProductId { get; }

        public Brush Color { get; }

        public static bool operator ==(ProductComparisonResult left, ProductComparisonResult right)
        {
            if (ReferenceEquals(left, null))
            {
                return ReferenceEquals(right, null);
            }

            return left.Equals(right);
        }

        public static bool operator !=(ProductComparisonResult left, ProductComparisonResult right)
        {
            return !(left == right);
        }

        public static bool operator <(ProductComparisonResult left, ProductComparisonResult right)
        {
            return ReferenceEquals(left, null) ? !ReferenceEquals(right, null) : left.CompareTo(right) < 0;
        }

        public static bool operator <=(ProductComparisonResult left, ProductComparisonResult right)
        {
            return ReferenceEquals(left, null) || left.CompareTo(right) <= 0;
        }

        public static bool operator >(ProductComparisonResult left, ProductComparisonResult right)
        {
            return !ReferenceEquals(left, null) && left.CompareTo(right) > 0;
        }

        public static bool operator >=(ProductComparisonResult left, ProductComparisonResult right)
        {
            return ReferenceEquals(left, null) ? ReferenceEquals(right, null) : left.CompareTo(right) >= 0;
        }

        public int CompareTo(ProductComparisonResult other)
        {
            if (other == null)
            {
                return -1;
            }

            if (deviationQuantity > other.deviationQuantity)
            {
                return 1;
            }

            if (deviationQuantity < other.deviationQuantity)
            {
                return -1;
            }

            return 0;
        }

        public int CompareTo(object obj)
        {
            return CompareTo(obj as ProductComparisonResult);
        }

        public override string ToString()
        {
            return Deviation;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            return false;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}