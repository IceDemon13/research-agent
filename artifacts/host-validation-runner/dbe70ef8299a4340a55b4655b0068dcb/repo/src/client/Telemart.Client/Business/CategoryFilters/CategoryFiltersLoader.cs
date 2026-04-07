using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Telemart.Client.Common.MvvmEnhancements;
using Telemart.Client.Controls.Accordion;
using Telemart.Client.Data.Requests.Features.Catalog;
using Telemart.Client.Data.Requests.Features.Catalog.TransferObjects.Filter;
using Telemart.Client.Data.Requests.Features.Warehouse;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.TransferObjects.Paging;
using Telemart.Client.TransferObjects.Warehouse;

namespace Telemart.Client.Business.CategoryFilters
{
    public sealed class CategoryFiltersLoader
    {
        public const string WarehouseItemName = "warehouses";
        public const string LabelItemName = "labels";
        public const string PriceItemName = "price";

        public CategoryFiltersLoader(IWebClient webClient)
        {
            WebClient = webClient;
        }

        private IWebClient WebClient { get; }

        public async Task<IEnumerable<RootAccordionItem>> QueryCategoryFiltersAsync(
            int categoryId,
            int contractorId,
            bool filterByPrices = true)
        {
            Task<QueryFiltersResponse> filtersTask = WebClient.ExecuteCatalogApiRequestAsync(new QueryCategoryFilters(categoryId, contractorId));
            Task<PagedResult<WarehouseDto>> warehousesTask = WebClient.ExecuteApiRequestAsync(new QueryWarehouses(), true);

            await Task.WhenAll(filtersTask, warehousesTask);

            return GetFilters(filtersTask.Result, warehousesTask.Result.Data, filterByPrices);
        }

        private static IEnumerable<RootAccordionItem> GetFilters(QueryFiltersResponse filters, IEnumerable<WarehouseDto> warehouses, bool filterByPrices)
        {
            if (filterByPrices)
            {
                int step = 1;

                yield return new RootAccordionItem(new RangeAccordionItem(
                    (double)filters.PriceMin,
                    (double)filters.PriceMax,
                    step,
                    "Цена",
                    true,
                    PriceItemName));
            }

            yield return new RootAccordionItem(new CheckedListAccordionItem(
                filters.Labels.Select(x => new ComboBoxItem(x.Id, x.Name.Trim())),
                "Ярлыки",
                false,
                LabelItemName));

            int[] warehousetypeIds = { WarehouseKind.Main.Id, WarehouseKind.Pickup.Id, WarehouseKind.ShowCase.Id, WarehouseKind.Assembly.Id };

            yield return new RootAccordionItem(new CheckedListAccordionItem(
                warehouses
                    .Where(x => x.Active == 1 && warehousetypeIds.Contains(x.TypeId))
                    .OrderByDescending(x => x.Position)
                    .Select(x => new ComboBoxItem(x.Id, x.Name)),
                "Склады",
                false,
                WarehouseItemName));

            foreach (FilterGroupDto filterGroup in filters.FilterGroups)
            {
                List<ComboBoxItem> filterItems = filterGroup.Filters
                    .Where(x => x.Active)
                    .Select(x => new ComboBoxItem(x.Id, x.Text.Trim()))
                    .ToList();

                if (filterItems.Any())
                {
                    yield return new RootAccordionItem(new CheckedListAccordionItem(
                        filterItems,
                        filterGroup.Name.Trim(),
                        filterGroup.Expanded));
                }
            }
        }
    }
}
