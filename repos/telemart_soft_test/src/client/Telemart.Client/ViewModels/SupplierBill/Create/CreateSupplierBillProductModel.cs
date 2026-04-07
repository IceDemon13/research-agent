using System;
using System.Collections.Generic;
using System.ComponentModel;
using DevExpress.Mvvm;
using DevExpress.Mvvm.DataAnnotations;
using Telemart.Client.Dictionaries;
using Telemart.Client.Properties;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.SupplierBill;
using Telemart.Common.PriceConversion;

namespace Telemart.Client.ViewModels.SupplierBill.Create
{
    public sealed class CreateSupplierBillProductModel : BindableBase, IDataErrorInfo
    {
        private const int round = 4;
        private readonly decimal price;
        private readonly bool priceWithTax;
        private decimal? invoiceProductPrice;
        private int? invoiceProductCurrencyId;
        private PriceEpsilonsDto priceEpsilonsDto;

        public CreateSupplierBillProductModel(string name, string code, int quantity, decimal price, bool priceWithTax, long? tnved)
        {
            Name = name;
            Code = code;
            Quantity = quantity;
            Tnved = tnved;

            this.price = price;
            this.priceWithTax = priceWithTax;
        }

        public string Name
        {
            get { return GetProperty(() => Name); }
            private set { SetProperty(() => Name, value); }
        }

        public string Code
        {
            get { return GetProperty(() => Code); }
            private set { SetProperty(() => Code, value); }
        }

        public int Quantity
        {
            get { return GetProperty(() => Quantity); }
            private set { SetProperty(() => Quantity, value); }
        }

        public TaxRate TaxRate
        {
            get { return GetProperty(() => TaxRate); }
            set { SetProperty(() => TaxRate, value, () => RaisePropertiesChanged(nameof(PriceWithTax), nameof(PriceNoTax), nameof(SumWithTax), nameof(SumNoTax))); }
        }

        public TaxRate TaxRateDefault
        {
            get { return GetProperty(() => TaxRateDefault); }
            set { SetProperty(() => TaxRateDefault, value); }
        }

        public decimal PriceWithTax => priceWithTax
            ? price
            : Math.Round(GetTaxPrice(), 2);

        public decimal PriceNoTax => priceWithTax
            ? Math.Round(GetNoTaxPrice(), 2)
            : price;

        public decimal SumWithTax => priceWithTax
            ? price * Quantity
            : Math.Round(GetTaxPrice() * Quantity, 2);

        public decimal SumNoTax => priceWithTax
            ? Math.Round(GetNoTaxPrice() * Quantity, 2)
            : price * Quantity;

        public int? ProductId
        {
            get { return GetProperty(() => ProductId); }
            set { SetProperty(() => ProductId, value); }
        }

        public int? InvoiceProductId
        {
            get { return GetProperty(() => InvoiceProductId); }
            set { SetProperty(() => InvoiceProductId, value); }
        }

        public string ProductName
        {
            get { return GetProperty(() => ProductName); }
            set { SetProperty(() => ProductName, value); }
        }

        public long? Tnved
        {
            get { return GetProperty(() => Tnved); }
            set { SetProperty(() => Tnved, value); }
        }

        #region IDataErrorInfo

        string IDataErrorInfo.Error => string.Empty;

        string IDataErrorInfo.this[string columnName] => IDataErrorInfoHelper.GetErrorText(this, columnName);

        #endregion

        public static void BuildMetadata(MetadataBuilder<CreateSupplierBillProductModel> builder)
        {
            builder.Property(x => x.ProductId).Required(() => Resources.RequiredErrorMessage);
            builder.Property(x => x.ProductName).Required(() => Resources.RequiredErrorMessage);
        }

        public void SetInvoiceProduct(InvoiceProductDto invoiceProduct)
        {
            InvoiceProductId = invoiceProduct.Id;

            invoiceProductPrice = invoiceProduct.Price;
            invoiceProductCurrencyId = invoiceProduct.CurrencyId;
        }

        public IEnumerable<string> ValidateInvoiceProduct(decimal? discountPercent, int selectedCurrencyId, PriceEpsilonsDto priceEpsilons, IPriceConverter priceConverter)
        {
            priceEpsilonsDto = priceEpsilons;

            bool isValid = true;

            if (InvoiceProductId == null)
            {
                yield return $"Товар \"{ProductName}\" не найден в накладной";
                isValid = false;
            }

            if (invoiceProductCurrencyId == null)
            {
                yield return $"У товара \"{ProductName}\" не задана валюта в накладной";
                isValid = false;
            }

            if (invoiceProductPrice == null)
            {
                yield return $"У товара \"{ProductName}\" не задана цена в накладной";
                isValid = false;
            }

            if (!isValid)
            {
                yield break;
            }

            decimal invoicePrice = discountPercent.HasValue
                ? invoiceProductPrice.Value - (invoiceProductPrice.Value * (discountPercent.Value / 100))
                : invoiceProductPrice.Value;

            if (selectedCurrencyId == Currency.UahId && invoiceProductCurrencyId == Currency.UahId)
            {
                if (Math.Abs(PriceWithTax - invoicePrice) > GetEpsilon(selectedCurrencyId))
                {
                    yield return $"У товара \"{ProductName}\" цена с НДС {PriceWithTax:c2}грн не соответствует цене в накладной {invoicePrice:c2}грн, погрешность {GetEpsilon(selectedCurrencyId)}грн";
                }
            }
            else if (selectedCurrencyId == Currency.UahId && invoiceProductCurrencyId != Currency.UahId)
            {
                decimal convertedPrice = priceConverter.Convert(
                    PriceWithTax,
                    selectedCurrencyId,
                    invoiceProductCurrencyId.Value,
                    CurrencyTypeIds.UsdMinusId,
                    false);

                if (Math.Abs(Math.Round(convertedPrice, round) - invoiceProductPrice.Value) > GetEpsilon(invoiceProductCurrencyId.Value))
                {
                    yield return $"У товара \"{ProductName}\" цена с НДС {PriceWithTax:c2}грн не соответствует цене в накладной {invoiceProductPrice.Value:c2}$, допустимая погрешность {GetEpsilon(invoiceProductCurrencyId.Value)}$";
                }
            }
            else if (selectedCurrencyId != Currency.UahId && invoiceProductCurrencyId != Currency.UahId && selectedCurrencyId == invoiceProductCurrencyId)
            {
                if (Math.Abs(invoicePrice - price) > GetEpsilon(selectedCurrencyId))
                {
                    yield return $"У товара \"{ProductName}\" цена в счете {price:c2}{Currency.GetById(selectedCurrencyId).Name} не соответствует цене в накладной {invoicePrice:c2}{Currency.GetById(invoiceProductCurrencyId.Value).Name}, погрешность {GetEpsilon(selectedCurrencyId)}{Currency.GetById(selectedCurrencyId).Name}";
                }
            }
            else
            {
                yield return $"Валюта в счете \"{Currency.GetById(selectedCurrencyId).Title}\" не соответствует валюте товара в накладной \"{Currency.GetById(invoiceProductCurrencyId.Value).Title}\"";
            }
        }

        private decimal GetEpsilon(int currencyId)
        {
            switch (currencyId)
            {
                case Currency.UahId:
                    return priceEpsilonsDto?.UahPriceEpsilon ?? 0;
                case Currency.UsdId:
                    return priceEpsilonsDto?.UsdPriceEpsilon ?? 0;
                case Currency.EurId:
                    return priceEpsilonsDto?.EurPriceEpsilon ?? 0;
                default: return 0;
            }
        }

        private decimal GetNoTaxPrice() => (100m * price) / (100m + TaxRate.Value);

        private decimal GetTaxPrice() => (price * (100m + TaxRate.Value)) / 100m;
    }
}