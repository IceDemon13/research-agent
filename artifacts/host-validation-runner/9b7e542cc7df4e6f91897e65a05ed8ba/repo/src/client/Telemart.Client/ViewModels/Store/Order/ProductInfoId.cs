using System;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.ViewModels.Store.Order
{
    public sealed class ProductInfoId : IEquatable<ProductInfoId>
    {
        public ProductInfoId(int productId, int currencyId, int? contractorId = null, TradeInProductInfoDto tradeInProductInfo = null)
        {
            ProductId = productId;
            CurrencyId = currencyId;
            ContractorId = contractorId;
            TradeInProductInfo = tradeInProductInfo;
        }

        public int ProductId { get; }

        public int CurrencyId { get; }

        public int? ContractorId { get; }

        public TradeInProductInfoDto TradeInProductInfo { get; }

        public static bool operator ==(ProductInfoId left, ProductInfoId right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(ProductInfoId left, ProductInfoId right)
        {
            return !Equals(left, right);
        }

        public bool Equals(ProductInfoId other)
        {
            if (other is null)
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return ProductId == other.ProductId && CurrencyId == other.CurrencyId && ContractorId == other.ContractorId;
        }

        public override bool Equals(object obj)
        {
            if (obj is null)
            {
                return false;
            }

            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            return obj is ProductInfoId other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = ProductId;
                hashCode = (hashCode * 397) ^ CurrencyId;
                hashCode = (hashCode * 397) ^ ContractorId.GetHashCode();
                return hashCode;
            }
        }
    }
}
