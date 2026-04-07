using System.Collections.Generic;
using System.Linq;
using Telemart.Client.TransferObjects;
using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.Store.Order
{
    public class SetOrderProductPriceViewItem : TelemartViewItemBase
    {
        public SetOrderProductPriceViewItem(
            int orderProductId,
            string productName,
            decimal price,
            int? priceId,
            IReadOnlyCollection<ProductPriceSimpleDto> prices,
            int currencyId,
            int quantity)
        {
            OrderProductId = orderProductId;
            ProductName = productName;
            PriceOld = price;
            PriceNew = price;
            PriceId = priceId;
            Prices = prices;
            CurrencyOldId = currencyId;
            Quantity = quantity;

            Delta = 0;
        }

        public SetOrderProductPriceViewItem()
        {
        }

        public int OrderProductId
        {
            get { return GetProperty(() => OrderProductId); }
            private init { SetProperty(() => OrderProductId, value); }
        }

        public string ProductName
        {
            get { return GetProperty(() => ProductName); }
            private init { SetProperty(() => ProductName, value); }
        }

        public int CurrencyOldId
        {
            get { return GetProperty(() => CurrencyOldId); }
            private init { SetProperty(() => CurrencyOldId, value); }
        }

        public int? MaxBonusesToUse
        {
            get { return GetProperty(() => MaxBonusesToUse); }
            private set { SetProperty(() => MaxBonusesToUse, value); }
        }

        public decimal PriceOld
        {
            get { return GetProperty(() => PriceOld); }
            private init { SetProperty(() => PriceOld, value); }
        }

        public decimal PriceNew
        {
            get { return GetProperty(() => PriceNew); }
            private set { SetProperty(() => PriceNew, value); }
        }

        public decimal Delta
        {
            get { return GetProperty(() => Delta); }
            private set { SetProperty(() => Delta, value); }
        }

        public int? PriceId
        {
            get { return GetProperty(() => PriceId); }
            private set { SetProperty(() => PriceId, value); }
        }

        public IReadOnlyCollection<ProductPriceSimpleDto> Prices
        {
            get { return GetProperty(() => Prices); }
            private init { SetProperty(() => Prices, value); }
        }

        public int Quantity
        {
            get { return GetProperty(() => Quantity); }
            private init { SetProperty(() => Quantity, value); }
        }

        /// <summary>
        /// Recalculate new price for current orderProduct.
        /// </summary>
        /// <param name="priceId">ProductPriceKind.</param>
        /// <returns>Returns error text. Null means success.</returns>
        public string RecalculatePriceNew(int priceId)
        {
            ProductPriceSimpleDto productPrice = Prices.FirstOrDefault(x => x.PriceTypeId == priceId);

            if (productPrice is null || productPrice.CurrencyId == 0)
            {
                return $"Для товара '{ProductName}' не заполнен выбранный тип цены.";
            }

            if (CurrencyOldId != productPrice.CurrencyId)
            {
                return $"У товара '{ProductName}' в выбранной цене не соответствует валюта.";
            }

            PriceNew = productPrice.Price;

            PriceId = priceId;
            Delta = PriceOld - PriceNew;
            MaxBonusesToUse = productPrice.MaxBonusesToUse;

            return null;
        }
    }
}