using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using Microsoft.Extensions.Logging;
using Telemart.Client.Common.Services;
using Telemart.Client.Core.Exceptions;
using Telemart.Client.Data.Requests.Features.Catalog;
using Telemart.Client.Data.Requests.Features.Parser;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Helpers;
using Telemart.Client.TransferObjects;
using Telemart.Client.ViewModels.Base;
using Telemart.Client.ViewModels.Nomenclature;

namespace Telemart.Client.ViewModels.Store.Order
{
    public sealed class OrderProductBulkAddViewModel : TelemartDialogViewModelBase
    {
        private OrderProductBulkAddParameter parameter;

        public OrderProductBulkAddViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            IMapper mapper,
            IBulkAddTextProcessor textProcessor)
            : base(webClient, dictionaries, messageFacadeService)
        {
            Mapper = mapper;
            TextProcessor = textProcessor;
            HandleTextCommand = new AsyncCommand(HandleTextAsync);
        }

        public OrderProductBulkAddViewModel()
        {
        }

        #region Commands

        public IAsyncCommand HandleTextCommand { get; }

        #endregion

        public ObservableCollection<OrderProductBulkViewItem> Items
        {
            get { return GetProperty(() => Items); }
            private set { SetProperty(() => Items, value); }
        }

        public IReadOnlyCollection<NomenclatureViewItem> NomenclatureItemsToAdd { get; private set; }

        public string RawText
        {
            get { return GetProperty(() => RawText); }
            set { SetProperty(() => RawText, value); }
        }

        #region DialogSettings

        public override int Height => 600;

        public override int MinHeight => 480;

        public override int MinWidth => 640;

        public override int Width => 800;

        #endregion

        private IMapper Mapper { get; }

        private IBulkAddTextProcessor TextProcessor { get; }

        protected override bool CanOk()
        {
            return Items != null && Items.Any(x => x.IsValid);
        }

        protected override Task HandleLoadedAsync()
        {
            Title = "Добавить из списка";
            return Task.CompletedTask;
        }

        protected override Task HandleOkAsync()
        {
            if (Items.Any(x => !x.IsValid))
            {
                if (!MessageFacadeService.Confirm("В списке есть нераспознанные товары. Добавить уже распознанные?"))
                {
                    return Task.CompletedTask;
                }
            }

            NomenclatureItemsToAdd = Items
                .Where(x => x.IsValid)
                .GroupBy(x => x.Product.Id)
                .Select(g => new { g.First().Product, Quantity = g.Select(y => y.Quantity).Sum() })
                .Select(y => MapToViewItem(y.Product, y.Quantity))
                .ToList();

            IsOk = true;
            Close();

            return Task.CompletedTask;
        }

        protected override void OnParameterChanged(object parameter)
        {
            if (IsInDesignMode)
            {
                return;
            }

            this.parameter = (OrderProductBulkAddParameter)parameter;
        }

        private NomenclatureViewItem MapToViewItem(ProductDto product, int quantity)
        {
            NomenclatureViewItem item = Mapper.Map<NomenclatureViewItem>(product);
            item.Quantity = quantity;
            return item;
        }

        private async Task HandleTextAsync()
        {
            if (string.IsNullOrWhiteSpace(RawText))
            {
                MessageFacadeService.ShowNotificationWarning("Поле не заполнено");
                return;
            }

            Items = null;

            try
            {
                IReadOnlyDictionary<string, int> items = TextProcessor.HandleText(RawText)
                    .GroupBy(x => x.Pattern)
                    .ToDictionary(g => g.Key, g => g.Sum(z => z.Quantity));

                if (!items.Any())
                {
                    MessageFacadeService.ShowNotificationWarning("В поле для ввода нет ни одной валидной строки");
                    return;
                }

                QueryProductByNames request = new QueryProductByNames(
                    parameter.ContractorId,
                    items.Keys.ToArray(),
                    Language.RussianId,
                    parameter.QueryGifts,
                    additionalServices: parameter.QueryAdditionalServices,
                    tags: new[] { "order_bulk_add_products" },
                    cartProductIds: parameter.CartProductIds,
                    take: 1);

                List<ProductSearchResponseDto> response = await WebClient.ExecuteCatalogApiRequestAsync(request);

                ProductSearchResponseDto[] recognizedItems = response.Where(x => x.Products?.Any() == true).ToArray();

                if (!recognizedItems.Any())
                {
                    MessageFacadeService.ShowNotificationWarning($"Ни один товар не распознан");
                    return;
                }

                if (parameter.PriceContext == NomenclatureViewPriceContext.Supplier)
                {
                    QueryContractorPrices queryContractorPricesRequest = new QueryContractorPrices(
                        parameter.ContractorId,
                        recognizedItems.Select(x => x.Products.First().Product.Id).Distinct().ToArray());

                    List<ParserContractorPriceDto> contractorPriceDtos = await WebClient.ExecuteApiRequestAsync(queryContractorPricesRequest);

                    Dictionary<int, ParserContractorPriceDto> contractorPrices = contractorPriceDtos.ToDictionary(x => x.ProductId);

                    foreach (ProductDto product in recognizedItems.Select(x => x.Products.First().Product))
                    {
                        if (contractorPrices.TryGetValue(product.Id, out ParserContractorPriceDto p))
                        {
                            product.Price = p.Price;
                            product.CurrencyId = p.CurrencyId;
                        }
                        else
                        {
                            product.Price = 0;
                            product.CurrencyId = Currency.GetByName(product.Currency).Id;
                        }
                    }
                }

                Items = items
                    .Select(y => new OrderProductBulkViewItem { ProductName = y.Key, Quantity = y.Value, Product = response.FirstOrDefault(x => x.Pattern == y.Key)?.Products.FirstOrDefault()?.Product })
                    .ToObservableCollection();
            }
            catch (UnexpectedSatusException exception)
            {
                Logger.LogError(exception, "Failed to search by products");
                MessageFacadeService.ShowNotificationError("Ошибка при поиске продуктов");
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Failed to handle products text");
                MessageFacadeService.ShowNotificationError("Ошибка при обработке текста");
            }
        }
    }
}