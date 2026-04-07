using System;
using System.ComponentModel;
using DevExpress.Mvvm;
using DevExpress.Mvvm.DataAnnotations;
using Telemart.Client.Dictionaries;
using Telemart.Client.Helpers;
using Telemart.Common.PriceConversion;
using TagColor = Telemart.Client.Dictionaries.TagColor;

namespace Telemart.Client.ViewModels.Directories.ProductsPrices
{
    public sealed class ProductPriceDataViewItem : BindableBase, IDataErrorInfo
    {
        private const decimal PriceChangeThreshold = 0.1m;

        private readonly int usdCurrency;
        private readonly ProductPriceKind kind;
        private readonly ProductPriceViewItem parent;
        private readonly int currencyId;
        private readonly decimal priceOld;
        private readonly decimal? pricePrevOld;
        private readonly int? maxBonusesToUseOld;
        private readonly int? partialPayOld;
        private readonly int? partialPayPbOld;
        private readonly int? partialPayPumbOld;
        private readonly int? partialPayAbOld;
        private readonly TagColor tagColorOld;

        private IPriceConverter priceConverter;
        private decimal price;
        private decimal? pricePrev;
        private decimal displayPrice;
        private decimal? displayPricePrev;
        private decimal displayPriceOld;
        private decimal displayPricePrevOld;

        public ProductPriceDataViewItem(
            IPriceConverter priceConverter,
            int usdCurrency,
            decimal price,
            decimal? pricePrev,
            int currencyId,
            ProductPriceKind kind,
            int? displayCurrency,
            int maxBonusesToUse,
            int? partialPay,
            int? partialPayPb,
            int? partialPayPumb,
            int? partialPayAb,
            ProductPriceViewItem parent,
            TagColor tagColor,
            string modifiedOn,
            double f2Markup,
            bool? contractorAllowDocument)
        {
            this.priceConverter = priceConverter;
            this.usdCurrency = usdCurrency;
            this.currencyId = currencyId;
            this.price = price;
            this.pricePrev = pricePrev;
            this.kind = kind;
            this.parent = parent;

            priceOld = price;
            pricePrevOld = pricePrev;
            ContractorAllowDocument = contractorAllowDocument;
            maxBonusesToUseOld = maxBonusesToUse;
            MaxBonusesToUse = maxBonusesToUse;
            PartialPay = partialPay;
            partialPayOld = partialPay;
            PartialPayPb = partialPayPb;
            partialPayPbOld = partialPayPb;
            PartialPayPumb = partialPayPumb;
            partialPayPumbOld = partialPayPumb;
            PartialPayAb = partialPayAb;
            partialPayAbOld = partialPayAb;

            DisplayCurrencyId = displayCurrency ?? currencyId;

            TagColor = tagColor;
            tagColorOld = tagColor;
            F2Markup = f2Markup;

            if (modifiedOn != null && DateTime.TryParse(modifiedOn, out DateTime result))
            {
                ModifiedOn = result;
            }
        }

        public int DisplayCurrencyId
        {
            get { return GetProperty(() => DisplayCurrencyId); }
            set { SetProperty(() => DisplayCurrencyId, value, OnDisplayCurrencyChanged); }
        }

        public int MaxBonusesToUse
        {
            get { return GetProperty(() => MaxBonusesToUse); }
            set { SetProperty(() => MaxBonusesToUse, value, () => RaisePropertyChanged(nameof(IsChanged))); }
        }

        public double F2Markup
        {
            get { return GetProperty(() => F2Markup); }
            set { SetProperty(() => F2Markup, value, () => RaisePropertyChanged(nameof(IsChanged))); }
        }

        public bool? ContractorAllowDocument
        {
            get { return GetProperty(() => ContractorAllowDocument); }
            set { SetProperty(() => ContractorAllowDocument, value, () => RaisePropertyChanged(nameof(IsChanged))); }
        }

        public int? PartialPay
        {
            get { return GetProperty(() => PartialPay); }
            set { SetProperty(() => PartialPay, value, () => RaisePropertyChanged(nameof(IsChanged))); }
        }

        public int? PartialPayPb
        {
            get { return GetProperty(() => PartialPayPb); }
            set { SetProperty(() => PartialPayPb, value, () => RaisePropertyChanged(nameof(IsChanged))); }
        }

        public int? PartialPayPumb
        {
            get { return GetProperty(() => PartialPayPumb); }
            set { SetProperty(() => PartialPayPumb, value, () => RaisePropertyChanged(nameof(IsChanged))); }
        }

        public int? PartialPayAb
        {
            get { return GetProperty(() => PartialPayAb); }
            set { SetProperty(() => PartialPayAb, value, () => RaisePropertyChanged(nameof(IsChanged))); }
        }

        public TagColor TagColor
        {
            get { return GetProperty(() => TagColor); }
            set { SetProperty(() => TagColor, value); }
        }

        public DateTime? ModifiedOn
        {
            get { return GetProperty(() => ModifiedOn); }
            set { SetProperty(() => ModifiedOn, value); }
        }

        public string PricePoliticName
        {
            get { return GetProperty(() => PricePoliticName); }
            set { SetProperty(() => PricePoliticName, value); }
        }

        public decimal DisplayPriceOld => displayPriceOld;

        public decimal DisplayPricePrevOld => displayPricePrevOld;

        public decimal DisplayPrice
        {
            get => displayPrice;
            set
            {
                price = priceConverter.Convert(value, DisplayCurrencyId, currencyId, usdCurrency);
                displayPrice = priceConverter.Convert(price, currencyId, DisplayCurrencyId, usdCurrency);

                RaisePropertiesChanged(
                    nameof(DisplayPrice),
                    nameof(ExtraCharge),
                    nameof(CompetitorPriceKoef),
                    nameof(PriceRose),
                    nameof(PriceDecreased),
                    nameof(IsChanged));
            }
        }

        public decimal? DisplayPricePrev
        {
            get => displayPricePrev;
            set
            {
                if (value is null)
                {
                    pricePrev = null;
                    displayPricePrev = null;
                }
                else
                {
                    pricePrev = priceConverter.Convert(value.Value, DisplayCurrencyId, currencyId, usdCurrency);
                    displayPricePrev = priceConverter.Convert(pricePrev ?? 0, currencyId, DisplayCurrencyId, usdCurrency);
                }

                RaisePropertiesChanged(
                    nameof(DisplayPricePrev),
                    nameof(ExtraCharge),
                    nameof(CompetitorPriceKoef),
                    nameof(PricePrevRose),
                    nameof(PricePrevDecreased),
                    nameof(IsChanged));
            }
        }

        public decimal? ExtraCharge
        {
            get
            {
                decimal? extraCharge = null;

                decimal p = GetPriceForCalculations();

                if (p > 0)
                {
                    decimal? priceInUsd = parent.PriceInUsd;
                    decimal priceUsd = priceConverter.Convert(p, currencyId, Currency.Usd.Id, usdCurrency);

                    extraCharge = ProductHelper.GetExtraCharge(priceInUsd, priceUsd);
                }

                return extraCharge;
            }
        }

        public decimal? CompetitorPriceKoef
        {
            get
            {
                decimal? k = null;

                decimal p = GetPriceForCalculations();

                if (p > 0 && parent.PriceCompetitor > 0)
                {
                    decimal priceCompetitorUsd = parent.PriceCompetitor.Value;
                    decimal priceUsd = priceConverter.Convert(p, currencyId, Currency.Usd.Id, usdCurrency);

                    k = priceUsd / priceCompetitorUsd;
                }

                return k;
            }
        }

        public bool? PriceRose
        {
            get
            {
                bool? res = null;

                decimal p = GetPriceForCalculations();

                if (priceOld > 0 && p > 0)
                {
                    decimal priceOldUsd = priceConverter.Convert(priceOld, currencyId, Currency.Usd.Id, usdCurrency);
                    decimal priceUsd = priceConverter.Convert(p, currencyId, Currency.Usd.Id, usdCurrency);

                    decimal f = priceUsd - priceOldUsd;

                    res = f > PriceChangeThreshold;
                }

                return res;
            }
        }

        public bool? PricePrevRose
        {
            get
            {
                bool? res = null;

                decimal p = GetPricePrevForCalculations();

                if (pricePrevOld > 0 && p > 0)
                {
                    decimal pricePrevOldUsd = priceConverter.Convert(pricePrevOld.Value, currencyId, Currency.Usd.Id, usdCurrency);
                    decimal pricePrevUsd = priceConverter.Convert(p, currencyId, Currency.Usd.Id, usdCurrency);

                    decimal f = pricePrevUsd - pricePrevOldUsd;

                    res = f > PriceChangeThreshold;
                }

                return res;
            }
        }

        public bool? PriceDecreased
        {
            get
            {
                bool? res = null;

                decimal p = GetPriceForCalculations();

                if (priceOld > 0 && p > 0)
                {
                    decimal priceOldUsd = priceConverter.Convert(priceOld, currencyId, Currency.Usd.Id, usdCurrency);
                    decimal priceUsd = priceConverter.Convert(p, currencyId, Currency.Usd.Id, usdCurrency);

                    decimal f = priceOldUsd - priceUsd;

                    res = f > PriceChangeThreshold;
                }

                return res;
            }
        }

        public bool? PricePrevDecreased
        {
            get
            {
                bool? res = null;

                decimal p = GetPricePrevForCalculations();

                if (pricePrevOld > 0 && p > 0)
                {
                    decimal pricePrevOldUsd = priceConverter.Convert(pricePrevOld.Value, currencyId, Currency.Usd.Id, usdCurrency);
                    decimal pricePrevUsd = priceConverter.Convert(p, currencyId, Currency.Usd.Id, usdCurrency);

                    decimal f = pricePrevOldUsd - pricePrevUsd;

                    res = f > PriceChangeThreshold;
                }

                return res;
            }
        }

        public bool IsChanged => price != priceOld
                                 || pricePrev != pricePrevOld
                                 || MaxBonusesToUse != maxBonusesToUseOld
                                 || TagColor != tagColorOld
                                 || PartialPay != partialPayOld
                                 || PartialPayPb != partialPayPbOld
                                 || PartialPayPumb != partialPayPumbOld
                                 || PartialPayAb != partialPayAbOld;

        public string Error => string.Empty;

        string IDataErrorInfo.this[string columnName] => IDataErrorInfoHelper.GetErrorText(this, columnName);

        public static void BuildMetadata(MetadataBuilder<ProductPriceDataViewItem> builder)
        {
            builder.Property(x => x.DisplayPrice)
                .MatchesInstanceRule(
                    (x, y) => y.GetPriceKind() == null || y.GetPriceKind().Id <= 0 || !y.GetAvailability().CanBuy || x > 0,
                    () => "Цена должна быть > 0");

            builder.Property(x => x.DisplayPricePrev)
                .MatchesInstanceRule(
                    (x, y) => x is null || y.GetPriceKind() == null || y.GetPriceKind().Id <= 0 || !y.GetAvailability().CanBuy || x > 0,
                    () => "Перечеркнутая цена должна быть > 0");

            builder.Property(x => x.MaxBonusesToUse)
                 .MatchesInstanceRule((x, y) => y.DisplayCurrencyId != Currency.UahId || y.DisplayPrice >= x, () => "Цена не может быть меньше либо равна максимального количества бонусов");

            builder.Property(x => x.PartialPay)
                 .InRange(3, 25, () => "Значение должно быть от 3 до 25");

            builder.Property(x => x.PartialPayPb)
                .InRange(1, 25, () => "Значение должно быть от 1 до 25");

            builder.Property(x => x.PartialPayPumb)
                .InRange(1, 25, () => "Значение должно быть от 1 до 25");

            builder.Property(x => x.PartialPayAb)
                .InRange(1, 25, () => "Значение должно быть от 1 до 25");
        }

        public void SetDisplayCurrency(int? displayCurrencyId)
        {
            DisplayCurrencyId = displayCurrencyId ?? currencyId;
        }

        public ProductPriceKind GetPriceKind()
        {
            return kind;
        }

        public decimal GetPriceValue()
        {
            return price;
        }

        public decimal? GetPricePrevValue()
        {
            return pricePrev;
        }

        public TagColor GetTagColor()
        {
            return TagColor;
        }

        public int? GetPartialPay()
        {
            return PartialPay;
        }

        public int? GetPartialPayPb()
        {
            return PartialPayPb;
        }

        public int? GetPartialPayPumb()
        {
            return PartialPayPumb;
        }
        
        public int? GetPartialPayAb()
        {
            return PartialPayAb;
        }

        public double GetPriceOldUsdValue()
        {
            return (double)priceConverter.Convert(priceOld, GetCurrencyId(), Currency.Usd.Id, usdCurrency);
        }

        public double GetPricePrevOldUsdValue()
        {
            if (pricePrevOld is null || pricePrevOld == 0)
            {
                return 0;
            }

            return (double)priceConverter.Convert(pricePrevOld.Value, GetCurrencyId(), Currency.Usd.Id, usdCurrency);
        }

        public double GetPriceUsdValue()
        {
            return (double)priceConverter.Convert(price, GetCurrencyId(), Currency.Usd.Id, usdCurrency);
        }

        public void SetPriceUsdValue(double value, double? pricePrev, IPriceConverter actualPriceConverter)
        {
            priceConverter = actualPriceConverter;

            DisplayPrice = priceConverter.Convert((decimal)value, Currency.Usd.Id, DisplayCurrencyId, usdCurrency);

            DisplayPricePrev = priceConverter.Convert((decimal)(pricePrev ?? 0), Currency.Usd.Id, DisplayCurrencyId, usdCurrency);
        }

        public ProductAvailability GetAvailability()
        {
            return parent.Avail;
        }

        private int GetCurrencyId()
        {
            return currencyId;
        }

        private decimal GetPriceForCalculations()
        {
            return parent.Avail.CanBuy ? price : 0m;
        }

        private decimal GetPricePrevForCalculations()
        {
            return parent.Avail.CanBuy && pricePrev.HasValue ? pricePrev.Value : 0m;
        }

        private void OnDisplayCurrencyChanged()
        {
            displayPriceOld = priceConverter.Convert(priceOld, currencyId, DisplayCurrencyId, usdCurrency);
            displayPrice = priceConverter.Convert(price, currencyId, DisplayCurrencyId, usdCurrency);

            if (pricePrevOld.HasValue)
            {
                displayPricePrevOld = priceConverter.Convert(pricePrevOld.Value, currencyId, DisplayCurrencyId, usdCurrency);
            }

            if (pricePrev.HasValue)
            {
                displayPricePrev = priceConverter.Convert(pricePrev.Value, currencyId, DisplayCurrencyId, usdCurrency);
            }

            RaisePropertiesChanged(
                nameof(DisplayPriceOld),
                nameof(DisplayPrice),
                nameof(DisplayPricePrevOld),
                nameof(DisplayPricePrev));
        }
    }
}