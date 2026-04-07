using System.Collections.Generic;

namespace Telemart.Client.ViewModels.Nomenclature
{
    public sealed class NomenclatureViewOptions
    {
        public NomenclatureViewOptions(
            NomenclatureViewPriceContext priceContext,
            int contractorId,
            NomenclatureViewSelectionMode selectionMode,
            bool showPrice = true,
            bool canEditPrice = false,
            bool excludeDiscounts = false,
            bool queryGifts = false,
            bool queryAdditionalServices = false,
            string searchText = "",
            bool fullScreenMode = false,
            int? selectedCategoryId = null,
            IReadOnlyCollection<ProductQuantity> compatibleWithProducts = null,
            bool queryPriceIn = false,
            bool priceInIncludeReserve = false,
            bool includePriceJson = false,
            int? additionalServiceProvideProductsByProductId = null,
            int[] cartProductIds = null,
            int[] productTypeIds = null)
        {
            PriceContext = priceContext;
            ContractorId = contractorId;
            SelectionMode = selectionMode;

            ShowPrice = showPrice;
            CanEditPrice = canEditPrice;
            ExcludeDiscounts = excludeDiscounts;
            QueryGifts = queryGifts;
            QueryAdditionalServices = queryAdditionalServices;
            SearchText = searchText;
            FullScreenMode = fullScreenMode;
            SelectedCategoryId = selectedCategoryId;
            CompatibleWithProducts = compatibleWithProducts;
            QueryPriceIn = queryPriceIn;
            PriceInIncludeReserve = priceInIncludeReserve;
            IncludePriceJson = includePriceJson;
            AdditionalServiceProvideProductsByProductId = additionalServiceProvideProductsByProductId;
            CartProductIds = cartProductIds;
            ProductTypeIds = productTypeIds;
        }

        public NomenclatureViewPriceContext PriceContext { get; }

        public int ContractorId { get; }

        public NomenclatureViewSelectionMode SelectionMode { get; }

        public bool ShowPrice { get; }

        public bool CanEditPrice { get; }

        public bool ExcludeDiscounts { get; }

        public bool QueryGifts { get; }

        public bool QueryAdditionalServices { get; }

        public bool QueryPriceIn { get; }

        public bool PriceInIncludeReserve { get; }

        public string SearchText { get; }

        public bool FullScreenMode { get; }

        public bool IncludePriceJson { get; }

        public int? SelectedCategoryId { get; }

        public IReadOnlyCollection<ProductQuantity> CompatibleWithProducts { get; }

        public int? AdditionalServiceProvideProductsByProductId { get; }

        public int[] CartProductIds { get; }

        public int[] ProductTypeIds { get; }
    }
}