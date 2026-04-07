using System.Collections.Generic;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.Promo;

namespace Telemart.Client.Business.Tag
{
    public sealed class TagPrintInfo
    {
        public TagPrintInfo(
            int productId,
            string productName,
            string productNameUkr,
            string link,
            string linkUkr,
            decimal productPrice,
            decimal productPricePrev,
            int formatId,
            int languageId,
            int? bonusAmount,
            IReadOnlyCollection<ProductFeatureGroupDto> features,
            CatalogPromoSimpleDto promo)
        {
            ProductId = productId;
            ProductName = productName;
            ProductNameUkr = productNameUkr;
            Link = link;
            LinkUkr = linkUkr;
            ProductPrice = productPrice;
            ProductPricePrev = productPricePrev;
            FormatId = formatId;
            Features = features;
            LanguageId = languageId;
            BonusAmount = bonusAmount;
            Promo = promo;
        }

        public int ProductId { get; }

        public string ProductName { get; }

        public string ProductNameUkr { get; }

        public int? BonusAmount { get; }

        public string Link { get; }

        public string LinkUkr { get; }

        public decimal ProductPrice { get; }

        public decimal ProductPricePrev { get; }

        public int FormatId { get; }

        public int LanguageId { get; }

        public bool ShowPricePrev => ProductPricePrev > ProductPrice;

        public IReadOnlyCollection<ProductFeatureGroupDto> Features { get; }

        public CatalogPromoSimpleDto Promo { get; }
    }
}