using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using Microsoft.Extensions.Logging;
using Telemart.Client.Common;
using Telemart.Client.Common.Messages;
using Telemart.Client.Common.Services;
using Telemart.Client.Data.Requests.Features.Contractor;
using Telemart.Client.Data.Requests.Features.Employee;
using Telemart.Client.Data.Requests.Features.Products;
using Telemart.Client.Data.Requests.Features.Warehouse;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Data.WebClient.Security;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.Showcase;
using Telemart.Client.TransferObjects.Warehouse;
using Telemart.Client.ViewModels.Common.Product;
using Telemart.Client.ViewModels.Showcase;
using Telemart.Client.WebClient.Prices;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.ViewModels.Store.Order.ProductInformation
{
    public class ProductInformationViewModel : ViewModelBase
    {
        private const string PriceToolTip = "Цена";
        private const string ContractorPricesToolTip = "Исходная / текущая цена";
        private const string UsdFormatter = "$0.00";
        private const string UahFormatter = "n0";
        private const int RequestProductTimerDelayMilliseconds = 350;
        private const int RequestIntervalMilliseconds = 650;

        private DispatcherTimer requestProductTimer;
        private DispatcherTimer requestIntervalTimer;

        private List<ContractorDto> contractorsList;
        private IReadOnlyDictionary<int, string> contractors;

        private List<EmployeeDto> employeesList;
        private IReadOnlyDictionary<int, string> employees;

        private List<WarehouseDto> warehousesList;
        private IReadOnlyDictionary<int, string> warehouses;

        private ProductInfoHotlineDto hotlineInfo;

        public ProductInformationViewModel(
            IPricesClient pricesClient,
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            IMessenger messenger,
            DocumentCommands documentCommands,
            ILogger<ProductInformationViewModel> logger)
            : this()
        {
            PricesClient = pricesClient;
            WebClient = webClient ?? throw new ArgumentNullException(nameof(webClient));
            Dictionaries = dictionaries ?? throw new ArgumentNullException(nameof(dictionaries));
            MessageFacadeService = messageFacadeService ?? throw new ArgumentNullException(nameof(messageFacadeService));
            DocumentCommands = documentCommands;
            Logger = logger;

            AbcTypes = Dictionaries.GetItems<AbcType>().ToReadOnlyObservableCollection();

            InitializeTimers();

            CanAddShowcase = WebClient.IsOperationAllowed(BusinessOperation.ShowcaseCreate);

            messenger.Register<ShowcaseMessage>(this, OnShowcaseMessage);
        }

        public ProductInformationViewModel()
        {
            SetCurrencyModeCommand = new DelegateCommand<CurrencyMode>(SetCurrencyMode);
            ShowProductInfoCommand = new DelegateCommand(ShowProductInfo);
            ShowHotlineCompetitorsCommand = new DelegateCommand(ShowHotlineCompetitors);
            ShowShowcaseCommand = new DelegateCommand<int?>(ShowShowcase, x => x.HasValue);
            CopyProductIdCommand = new DelegateCommand(CopyProductId);

            SelectedCurrencyMode = CurrencyMode.Auto;
        }

        public IDelegateCommand SetCurrencyModeCommand { get; }

        public IDelegateCommand ShowProductInfoCommand { get; }

        public IDelegateCommand CopyProductIdCommand { get; }

        public IDelegateCommand ShowHotlineCompetitorsCommand { get; }

        public IDelegateCommand ShowShowcaseCommand { get; }

        #region INPC

        public bool IsLoadingInProgress
        {
            get { return GetProperty(() => IsLoadingInProgress); }
            private set { SetProperty(() => IsLoadingInProgress, value); }
        }

        public ReadOnlyObservableCollection<AbcType> AbcTypes
        {
            get { return GetProperty(() => AbcTypes); }
            private set { SetProperty(() => AbcTypes, value); }
        }

        public ReadOnlyObservableCollection<ProductContractorPriceViewItem> Competitors
        {
            get { return GetProperty(() => Competitors); }
            private set { SetProperty(() => Competitors, value, () => { RaisePropertyChanged(nameof(CompetitorsVisible)); }); }
        }

        public ReadOnlyObservableCollection<ProductLeftoverViewItem> Leftovers
        {
            get { return GetProperty(() => Leftovers); }
            private set { SetProperty(() => Leftovers, value, () => { RaisePropertyChanged(nameof(LeftoversVisible)); }); }
        }

        public ReadOnlyObservableCollection<ProductLeftoverReserveViewItem> LeftoversReserve
        {
            get { return GetProperty(() => LeftoversReserve); }
            private set { SetProperty(() => LeftoversReserve, value, () => { RaisePropertyChanged(nameof(LeftoversReserveVisible)); }); }
        }

        public ReadOnlyObservableCollection<ProductInfoWhiteStockViewItem> WhiteStocks
        {
            get { return GetProperty(() => WhiteStocks); }
            private set { SetProperty(() => WhiteStocks, value, () => { RaisePropertyChanged(nameof(WhiteStocksVisible)); }); }
        }

        public ReadOnlyObservableCollection<ProductTransitViewItem> Transits
        {
            get { return GetProperty(() => Transits); }
            private set { SetProperty(() => Transits, value, () => { RaisePropertyChanged(nameof(TransitsVisible)); }); }
        }

        public ReadOnlyObservableCollection<ProductMovementViewItem> Movements
        {
            get { return GetProperty(() => Movements); }
            private set { SetProperty(() => Movements, value, () => { RaisePropertyChanged(nameof(MovementsVisible)); }); }
        }

        public ReadOnlyObservableCollection<ProductInfoPriceViewItem> Prices
        {
            get { return GetProperty(() => Prices); }
            private set { SetProperty(() => Prices, value, () => { RaisePropertyChanged(nameof(PricesVisible)); }); }
        }

        public ReadOnlyObservableCollection<ProductInfoServiceViewItem> ServiceItems
        {
            get { return GetProperty(() => ServiceItems); }
            private set { SetProperty(() => ServiceItems, value, () => { RaisePropertyChanged(nameof(ServiceItemsVisible)); }); }
        }

        public ObservableCollection<ProductShowcaseViewItem> Showcases
        {
            get { return GetProperty(() => Showcases); }
            private set { SetProperty(() => Showcases, value, () => { RaisePropertyChanged(nameof(ShowcasesVisible)); }); }
        }

        public ObservableCollection<ProductInfoSearchTemplateViewItem> SearchTemplates
        {
            get { return GetProperty(() => SearchTemplates); }
            private set { SetProperty(() => SearchTemplates, value, () => { RaisePropertyChanged(nameof(SearchTemplatesVisible)); }); }
        }

        public CurrencyMode SelectedCurrencyMode
        {
            get { return GetProperty(() => SelectedCurrencyMode); }
            private set { SetProperty(() => SelectedCurrencyMode, value); }
        }

        public Currency SelectedCurrency
        {
            get { return GetProperty(() => SelectedCurrency); }
            private set { SetProperty(() => SelectedCurrency, value); }
        }

        public string ProductName
        {
            get { return GetProperty(() => ProductName); }
            private set { SetProperty(() => ProductName, value); }
        }

        public decimal PriceTelemartUsd
        {
            get { return GetProperty(() => PriceTelemartUsd); }
            private set { SetProperty(() => PriceTelemartUsd, value); }
        }

        public ProductInfoId ProductId
        {
            get
            {
                return GetProperty(() => ProductId);
            }

            set
            {
                SetProperty(() => ProductId, value);

                // When first click occurs, go to server
                if (!requestIntervalTimer.IsEnabled)
                {
                    SetProduct();
                    TimerStartNew(requestIntervalTimer);
                }

                // if second click occured in requestIntervalTimer.Interval, perform product request with timer
                else
                {
                    IsLoadingInProgress = true;
                    TimerStartNew(requestProductTimer);
                }
            }
        }

        public ReadOnlyObservableCollection<ProductPurchaseHistoryViewItem> PurchaseHistory
        {
            get { return GetProperty(() => PurchaseHistory); }
            private set { SetProperty(() => PurchaseHistory, value, () => { RaisePropertyChanged(nameof(PurchaseHistoryVisible)); }); }
        }

        public ReadOnlyObservableCollection<ProductSalesHistoryViewItem> ContractorSalesHistory
        {
            get { return GetProperty(() => ContractorSalesHistory); }
            private set { SetProperty(() => ContractorSalesHistory, value, () => { RaisePropertyChanged(nameof(ContractorSalesHistoryVisible)); }); }
        }

        public ReadOnlyObservableCollection<ProductSalesHistoryViewItem> TelemartSalesHistory
        {
            get { return GetProperty(() => TelemartSalesHistory); }
            private set { SetProperty(() => TelemartSalesHistory, value, () => { RaisePropertyChanged(nameof(TelemartSalesHistoryVisible)); }); }
        }

        public ReadOnlyObservableCollection<ProductContractorPriceViewItem> Suppliers
        {
            get { return GetProperty(() => Suppliers); }
            private set { SetProperty(() => Suppliers, value, () => { RaisePropertyChanged(nameof(SuppliersVisible)); }); }
        }

        public ReadOnlyObservableCollection<ProductContractorPriceViewItem> Rrp
        {
            get { return GetProperty(() => Rrp); }
            private set { SetProperty(() => Rrp, value, () => { RaisePropertyChanged(nameof(RrpVisible)); }); }
        }

        public ReadOnlyObservableCollection<ViewSummaryDto> ViewsSummary
        {
            get { return GetProperty(() => ViewsSummary); }
            private set { SetProperty(() => ViewsSummary, value, () => { RaisePropertyChanged(nameof(ViewsSummaryVisible)); }); }
        }

        public ReadOnlyObservableCollection<SummaryViewItem> ViewsInfo
        {
            get { return GetProperty(() => ViewsInfo); }
            private set { SetProperty(() => ViewsInfo, value, () => { RaisePropertyChanged(nameof(ViewsInfoVisible)); }); }
        }

        public ReadOnlyObservableCollection<ProductInfoHyperlinkSummaryViewItem> HotlineInfo
        {
            get { return GetProperty(() => HotlineInfo); }
            private set { SetProperty(() => HotlineInfo, value, () => { RaisePropertyChanged(nameof(HotlineInfoVisible)); }); }
        }

        public ReadOnlyObservableCollection<ProductInfoPurchasePriceStatsViewItem> PurchasePriceStats
        {
            get { return GetProperty(() => PurchasePriceStats); }
            private set { SetProperty(() => PurchasePriceStats, value, () => { RaisePropertyChanged(nameof(PurchasePriceStatsVisible)); }); }
        }

        public ReadOnlyObservableCollection<ProductHotlineCompetitorViewItem> HotlineCompetitorPrices
        {
            get { return GetProperty(() => HotlineCompetitorPrices); }
            private set { SetProperty(() => HotlineCompetitorPrices, value, () => { RaisePropertyChanged(nameof(HotlineCompetitorPricesVisible)); }); }
        }

        #endregion

        public DocumentCommands DocumentCommands { get; }

        public bool CompetitorsVisible => (Competitors != null && Competitors.Any()) || IsInDesignMode;

        public bool TransitsVisible => (Transits != null && Transits.Any()) || IsInDesignMode;

        public bool MovementsVisible => (Movements != null && Movements.Any()) || IsInDesignMode;

        public bool LeftoversVisible => (Leftovers != null && Leftovers.Any()) || IsInDesignMode;

        public bool LeftoversReserveVisible => (LeftoversReserve != null && LeftoversReserve.Any()) || IsInDesignMode;

        public bool WhiteStocksVisible => (WhiteStocks != null && WhiteStocks.Any()) || IsInDesignMode;

        public bool ContractorSalesHistoryVisible => (ContractorSalesHistory != null && ContractorSalesHistory.Any()) || IsInDesignMode;

        public bool TelemartSalesHistoryVisible => (TelemartSalesHistory != null && TelemartSalesHistory.Any()) || IsInDesignMode;

        public bool SuppliersVisible => (Suppliers != null && Suppliers.Any()) || IsInDesignMode;

        public bool PricesVisible => (Prices != null && Prices.Any()) || IsInDesignMode;

        public bool ServiceItemsVisible => (ServiceItems != null && ServiceItems.Any()) || IsInDesignMode;

        public bool RrpVisible => (Rrp != null && Rrp.Any()) || IsInDesignMode;

        public bool ShowcasesVisible => (Showcases != null && Showcases.Any()) || IsInDesignMode || (CanAddShowcase && ProductId != null);

        public bool SearchTemplatesVisible => (SearchTemplates != null && SearchTemplates.Any()) || IsInDesignMode;

        public bool ViewsSummaryVisible => (ViewsSummary != null && ViewsSummary.Any()) || IsInDesignMode;

        public bool ViewsInfoVisible => ViewsInfo?.Any() == true || IsInDesignMode;

        public bool PurchaseHistoryVisible => (PurchaseHistory != null && PurchaseHistory.Any()) || IsInDesignMode;

        public bool HotlineInfoVisible => HotlineInfo?.Any() == true || IsInDesignMode;

        public bool PurchasePriceStatsVisible => PurchasePriceStats?.Any() == true || IsInDesignMode;

        public bool HotlineCompetitorPricesVisible => HotlineCompetitorPrices?.Any() == true || IsInDesignMode;

        public bool HotlineCompetitorsButtonVisible => WebClient.IsOperationAllowed(BusinessOperation.HotlineCompetitorUpdate);

        public bool CanAddShowcase { get; }

        private IPricesClient PricesClient { get; }

        private IWebClient WebClient { get; }

        private IDictionaries Dictionaries { get; }

        private IMessageFacadeService MessageFacadeService { get; }

        private ILogger<ProductInformationViewModel> Logger { get; }

        private IDocumentManagerService SizeableDialogDocumentManagerService => GetService<IDocumentManagerService>("SizeableDialogDocumentManagerService", ServiceSearchMode.PreferParents);

        private IDocumentManagerService DialogDocumentManagerService => GetService<IDocumentManagerService>("DialogDocumentManagerService");

        public void ClearProduct()
        {
            ProductName = "...";
            Leftovers = null;
            LeftoversReserve = null;
            WhiteStocks = null;
            Suppliers = null;
            Rrp = null;
            Competitors = null;
            PurchaseHistory = null;
            ContractorSalesHistory = null;
            ViewsSummary = null;
            Transits = null;
            Movements = null;
            ViewsInfo = null;
            HotlineInfo = null;
            HotlineCompetitorPrices = null;
            Prices = null;
            ServiceItems = null;
            Showcases = null;
            SearchTemplates = null;
        }

        public void SetDisplayCurrency(Currency currency)
        {
            SelectedCurrency = currency;
            SetDisplayPrices();
        }

        private static IEnumerable<ProductInfoServiceViewItem> MapServiceItems(TradeInPriceCalculatorResultDto tradeInPrice)
        {
            if (tradeInPrice.CurrentTradeInPrice > 0)
            {
                yield return new ProductInfoServiceViewItem("Trade-In", tradeInPrice.CurrentTradeInPrice.ToString("f2", CultureInfo.InvariantCulture));
            }
        }

        private static IEnumerable<ProductInfoHyperlinkSummaryViewItem> MapHotlineInfo(ProductInfoHotlineDto info)
        {
            if (info == null)
            {
                yield break;
            }

            yield return new ProductInfoHyperlinkSummaryViewItem("Название", info.Name, info.Link);
            yield return new ProductInfoHyperlinkSummaryViewItem("Рейтинг", $"{info.Position}");
            yield return new ProductInfoHyperlinkSummaryViewItem("Отзывов", $"{info.CommentCount}");
            yield return new ProductInfoHyperlinkSummaryViewItem("Предложений", $"{info.OfferCount}");
            yield return new ProductInfoHyperlinkSummaryViewItem("Мин. цена", info.DisplayMinPrice);
            yield return new ProductInfoHyperlinkSummaryViewItem("Цена", info.DisplayPrice);
        }

        private static IEnumerable<ProductInfoPurchasePriceStatsViewItem> MapPurchasePriceStats(IReadOnlyCollection<ProductInfoPurchasePriceStatsDto> purchaseStats)
        {
            if (purchaseStats?.Any() == true)
            {
                foreach (ProductInfoPurchasePriceStatsDto purchaseStat in purchaseStats)
                {
                    yield return new ProductInfoPurchasePriceStatsViewItem(
                        purchaseStat.MinPriceUsd,
                        purchaseStat.MaxPriceUsd,
                        purchaseStat.AveragePriceUsd,
                        purchaseStat.MinPriceUah,
                        purchaseStat.MaxPriceUah,
                        purchaseStat.AveragePriceUah,
                        purchaseStat.Label);
                }
            }
        }

        private static IEnumerable<ProductHotlineCompetitorViewItem> MapHotlineCompetitors(ProductInfoHotlineCompetitorPriceDto[] info)
        {
            if (info == null)
            {
                return Enumerable.Empty<ProductHotlineCompetitorViewItem>();
            }

            return info
                .Select(x => new ProductHotlineCompetitorViewItem(x.Name, x.AbcId, x.Ratio, x.PriceUah, x.PriceUsd))
                .OrderBy(x => x.PriceUsd)
                .ThenBy(x => x.PriceUah);
        }

        private static IEnumerable<ProductSalesHistoryViewItem> MapSalesHistory(IReadOnlyCollection<ProductSalesHistoryItemDto> sales, ProductInfoContractorSalesHistoryDto lastSale)
        {
            if (sales != null && sales.Any())
            {
                foreach (ProductSalesHistoryItemDto x in sales)
                {
                    yield return new ProductSalesHistoryViewItem(
                        x.Label,
                        x.LastSaleDateTime,
                        x.PriceUah,
                        x.PriceUsd,
                        x.TotalQuantity,
                        x.LastSale);
                }
            }

            if (lastSale != null)
            {
                yield return new ProductSalesHistoryViewItem(
                    lastSale.Label,
                    lastSale.DateTime,
                    lastSale.PriceUah,
                    lastSale.PriceUsd,
                    lastSale.Quantity);
            }
        }

        private static IEnumerable<ProductPurchaseHistoryViewItem> MapPurchaseHistory(IEnumerable<ProductPurchaseHistoryItemDto> purchaseHistory)
        {
            IEnumerable<ProductPurchaseHistoryViewItem> mappedHistory;

            if (purchaseHistory == null)
            {
                mappedHistory = Array.Empty<ProductPurchaseHistoryViewItem>();
            }
            else
            {
                mappedHistory = purchaseHistory.Select(x =>
                    new ProductPurchaseHistoryViewItem
                    {
                        ContractorId = x.ContractorId,
                        InvoiceId = x.InvoiceId,
                        CurrencyId = x.CurrencyId,
                        PriceUsd = x.PriceUsd,
                        PriceUah = x.PriceUah,
                        Quantity = x.Quantity,
                        ContractorName = x.ContractorName,
                        DateClose = x.DateClose,
                        LastPurchase = x.LastPurchase
                    });
            }

            return mappedHistory;
        }

        private static IEnumerable<ProductInfoPriceViewItem> MapPrices(IEnumerable<ProductInfoPriceDto> prices)
        {
            return prices?.Select(x => new ProductInfoPriceViewItem(x.Name, x.ModifiedOn, x.Uah, x.Usd)) ?? Enumerable.Empty<ProductInfoPriceViewItem>();
        }

        private static IEnumerable<ProductLeftoverViewItem> MapLeftovers(IEnumerable<ProductLeftoversDto> leftovers)
        {
            return leftovers?.Select(x => new ProductLeftoverViewItem
            {
                ReservedQuantity = x.ReservedByOrders + x.ReservedByReturnInvoices + x.ReservedByAssemblyComplectation + x.ReservedByMovement,
                PriceUsd = x.PriceUsd,
                PriceUah = x.PriceUah,
                WarehouseId = x.WarehouseId,
                WarehouseItems = x.WarehouseItems,
                WarehouseName = x.WarehouseName,
                WarehousePosition = x.WarehousePosition
            }) ?? Enumerable.Empty<ProductLeftoverViewItem>();
        }

        private static IEnumerable<ProductLeftoverReserveViewItem> MapLeftoversReserve(IEnumerable<ProductLeftoversDto> leftovers)
        {
            return leftovers?
                .Where(x => x.ReservedByOrders > 0 || x.ReservedByReturnInvoices > 0 || x.ReservedByAssemblyComplectation > 0 || x.ReservedByMovement > 0)
                .Select(x => new ProductLeftoverReserveViewItem
                {
                    AssembledComputerRuleReserveQuantity = x.ReservedByAssemblyComplectation,
                    OrderReserveQuantity = x.ReservedByOrders,
                    ReturnInvoiceReserveQuantity = x.ReservedByReturnInvoices,
                    WarehouseId = x.WarehouseId,
                    WarehouseName = x.WarehouseName,
                    WarehousePosition = x.WarehousePosition,
                    ReservedByMovement = x.ReservedByMovement
                }) ?? Enumerable.Empty<ProductLeftoverReserveViewItem>();
        }

        private static IEnumerable<ProductInfoWhiteStockViewItem> MapWhiteStocks(IEnumerable<ProductInfoWhiteStockDto> records)
        {
            return records?.Select(x => new ProductInfoWhiteStockViewItem
            {
                OrganizationName = x.OrganizationName == "Телемарт" ? "ТМР" : x.OrganizationName,
                Quantity = x.Quantity,
                PriceUah = x.PriceUah,
                PriceUsd = x.PriceUsd,
                QuantityFree = x.QuantityFree
            }) ?? Enumerable.Empty<ProductInfoWhiteStockViewItem>();
        }

        private static IEnumerable<ProductShowcaseViewItem> MapShowcases(IEnumerable<ProductInfoShowcaseDto> records)
        {
            return records?.Select(x => new ProductShowcaseViewItem
            {
                WarehouseName = x.WarehouseName,
                Id = x.Id,
                Capacity = x.Capacity,
                Active = x.Active,
            }) ?? Enumerable.Empty<ProductShowcaseViewItem>();
        }

        private static IEnumerable<ProductInfoSearchTemplateViewItem> MapSearchTemplates(IEnumerable<ProductInfoSearchTemplateDto> records)
        {
            return records?.Select(x => new ProductInfoSearchTemplateViewItem
            {
                SearchTemplateId = x.SearchTemplateId,
                SearchTemplateName = x.SearchTemplateName,
                MinPriceUah = x.MinPriceUah,
                MinPriceUsd = x.MinPriceUsd,
                AvgPriceUah = x.AvgPriceUah,
                AvgPriceUsd = x.AvgPriceUsd,
                MaxPriceUah = x.MaxPriceUah,
                MaxPriceUsd = x.MaxPriceUsd
            }) ?? Enumerable.Empty<ProductInfoSearchTemplateViewItem>();
        }

        private static void TimerStartNew(DispatcherTimer timer)
        {
            timer.Stop();
            timer.Start();
        }

        private void CopyProductId()
        {
            if (ProductId != null)
            {
                Clipboard.SetText(ProductId.ProductId.ToString());
            }
        }

        private ProductContractorPriceViewItem MapContractorPrice(ProductInfoContractorPriceDto source, ProductContractorPriceViewItem destination, decimal? extraCharge)
        {
            destination.Avail = source.Avail;
            destination.Consider = source.Consider;
            destination.ContractorId = source.ContractorId;
            destination.ContractorName = source.ContractorName;
            destination.DateAdd = source.DateAdd;
            destination.Link = source.Link;
            destination.PriceUah = source.PriceUah;
            destination.PriceUsd = source.PriceUsd;
            destination.SourcePriceUah = source.SourcePriceUah;
            destination.SourcePriceUsd = source.SourcePriceUsd;

            destination.ExtraCharge = extraCharge;

            if (Uri.TryCreate(destination.Link, UriKind.Absolute, out Uri uri) && !uri.IsWellFormedOriginalString())
            {
                destination.Link = $"http://{uri.Host}{uri.PathAndQuery}";
            }

            return destination;
        }

        private async Task<ReadOnlyObservableCollection<ProductContractorPriceViewItem>> MapContractorPricesAsync(IReadOnlyCollection<ProductInfoContractorPriceDto> contractorPrices)
        {
            if (contractorPrices == null || contractorPrices.Count == 0)
            {
                return null;
            }

            foreach (ProductInfoContractorPriceDto contractorPrice in contractorPrices)
            {
                contractorPrice.ContractorId = contractorPrice.ContractorId == 0 ? Constants.TelemartContractorId : contractorPrice.ContractorId;
            }

            CalculateExtraChargeDto calculateExtraCharge = new(
                contractorPrices.Select(
                    x => new CalculateExtraChargeProductDto(
                        ProductId.ProductId,
                        x.PriceUsd,
                        PriceTelemartUsd,
                        x.ContractorId)).ToArray());

            Result<IReadOnlyCollection<ContractorProductExtraChargeDto>> calculateExtraChargeResult = await PricesClient
                .CalculateExtraChargeAsync(calculateExtraCharge, this, CancellationToken.None);

            ReadOnlyObservableCollection<ProductContractorPriceViewItem> result = contractorPrices
                .Select(
                    x => MapContractorPrice(
                        x,
                        ProductContractorPriceViewItem.Create(),
                        calculateExtraChargeResult?.Data?.FirstOrDefault(z => z.ProductId == ProductId.ProductId && z.ContractorId == x.ContractorId)?.ExtraCharge))
                .ToReadOnlyObservableCollection();

            List<decimal> prices = contractorPrices
                .Where(x => !x.ContractorName.Equals("склад", StringComparison.InvariantCultureIgnoreCase))
                .OrderBy(x => x.PriceUah)
                .GroupBy(x => x.PriceUah)
                .Select(group => group.First().PriceUah)
                .Where(x => x > 0)
                .Take(2)
                .ToList();

            bool firstTime = true;

            foreach (decimal price in prices)
            {
                if (firstTime)
                {
                    result.Where(x => x.PriceUah.Equals(price)).ForEach(x => x.IsLowestPrice = true);
                    firstTime = false;
                }
                else
                {
                    result.Where(x => x.PriceUah.Equals(price)).ForEach(x => x.IsSecondLowestPrice = true);
                }
            }

            List<decimal?> extraCharges = result
                .Where(x => !x.ContractorName.Equals("склад", StringComparison.InvariantCultureIgnoreCase))
                .OrderByDescending(x => x.ExtraCharge)
                .GroupBy(x => x.ExtraCharge)
                .Select(group => group.First().ExtraCharge)
                .Where(x => x > 0)
                .Take(2)
                .ToList();

            firstTime = true;

            foreach (decimal? extraCharge in extraCharges)
            {
                if (extraCharge is null)
                {
                    continue;
                }

                if (firstTime)
                {
                    result.Where(x => x.ExtraCharge.Equals(extraCharge)).ForEach(x => x.IsHighestExtraCharge = true);
                    firstTime = false;
                }
                else
                {
                    result.Where(x => x.ExtraCharge.Equals(extraCharge)).ForEach(x => x.IsSecondHighestExtraCharge = true);
                }
            }

            return result;
        }

        private async Task RefreshWarehousesAsync()
        {
            List<WarehouseDto> list = await WebClient.ExecuteApiRequestAsync(new QueryWarehouses(), true).GetPagedResultDataAsync();

            if (ReferenceEquals(warehousesList, list))
            {
                return;
            }

            warehousesList = list;

            warehouses = warehousesList.ToDictionary(x => x.Id, x => x.Name);
        }

        private async Task RefreshContractorsAsync()
        {
            List<ContractorDto> list = await WebClient.ExecuteApiRequestAsync(new QueryContractors(), true).GetPagedResultDataAsync();

            if (ReferenceEquals(contractorsList, list))
            {
                return;
            }

            contractorsList = list;
            contractors = contractorsList.ToDictionary(x => x.Id, x => x.Name);
        }

        private async Task RefreshEmployeesAsync()
        {
            List<EmployeeDto> list = await WebClient.ExecuteApiRequestAsync(new QueryEmployees(), true).GetPagedResultDataAsync();

            if (ReferenceEquals(employeesList, list))
            {
                return;
            }

            employeesList = list;
            employees = employeesList.ToDictionary(x => x.Id, x => x.Name);
        }

        private async Task SetProductAsync(ProductInfoId productId)
        {
            if (productId == null)
            {
                return;
            }

            ClearProduct();

            IsLoadingInProgress = true;
            bool setLoadingTo = false;

            try
            {
                ProductInfoDto productInfo = null;

                try
                {
                    productInfo = await GetProductInfoAsync(productId.ProductId, productId.ContractorId);
                }
                catch (Exception)
                {
                    if (WebClient.IsOperationAllowed(BusinessOperation.Intern))
                    {
                        return;
                    }

                    throw;
                }

                if (productInfo.ProductId != ProductId.ProductId)
                {
                    setLoadingTo = true;
                    return;
                }

                PriceTelemartUsd = productInfo.PriceTelemartUsd;

                Task<ReadOnlyObservableCollection<ProductContractorPriceViewItem>> mapSupplierPricesTask = MapContractorPricesAsync(productInfo.Suppliers);
                Task<ReadOnlyObservableCollection<ProductContractorPriceViewItem>> mapRrpTask = MapContractorPricesAsync(productInfo.Rrp);
                Task<ReadOnlyObservableCollection<ProductContractorPriceViewItem>> mapCompetitorsTask = MapContractorPricesAsync(productInfo.Competitors);

                await Task.WhenAll(RefreshContractorsAsync(), RefreshEmployeesAsync(), RefreshWarehousesAsync(), mapSupplierPricesTask, mapRrpTask, mapCompetitorsTask);

                ProductName = productInfo.ProductName;
                Leftovers = MapLeftovers(productInfo.Leftovers).ToReadOnlyObservableCollection();
                LeftoversReserve = MapLeftoversReserve(productInfo.Leftovers).ToReadOnlyObservableCollection();
                WhiteStocks = MapWhiteStocks(productInfo.WhiteStocks).ToReadOnlyObservableCollection();
                Transits = MapTransits(productInfo.Transits).ToReadOnlyObservableCollection();
                Movements = MapMovements(productInfo.Movements).ToReadOnlyObservableCollection();
                Suppliers = mapSupplierPricesTask.Result;
                Rrp = mapRrpTask.Result;
                Competitors = mapCompetitorsTask.Result;
                PurchasePriceStats = MapPurchasePriceStats(productInfo.PurchasePriceStats).ToReadOnlyObservableCollection();
                PurchaseHistory = MapPurchaseHistory(productInfo.Purchases?.Where(x => x.Quantity > 0)).ToReadOnlyObservableCollection();
                ContractorSalesHistory = MapSalesHistory(productInfo.ContractorSalesHistory, productInfo.LastContractorSale).ToReadOnlyObservableCollection();
                TelemartSalesHistory = MapSalesHistory(productInfo.TelemartSalesHistory, productInfo.LastTelemartSale).ToReadOnlyObservableCollection();
                ViewsInfo = MapViewInfo(productInfo).ToReadOnlyObservableCollection();
                HotlineCompetitorPrices = MapHotlineCompetitors(productInfo.Hotline?.HotlineCompetitorPrices).ToReadOnlyObservableCollection();
                Prices = MapPrices(productInfo.Prices).ToReadOnlyObservableCollection();
                Showcases = MapShowcases(productInfo.Showcases).ToObservableCollection();
                SearchTemplates = MapSearchTemplates(productInfo.SearchTemplates).ToObservableCollection();
                ViewsSummary = productInfo.Summary != null
                    ? new[] { new ViewSummaryDto { Summary = productInfo.Summary } }.ToReadOnlyObservableCollection()
                    : null;

                RaisePropertiesChanged(
                    nameof(Leftovers),
                    nameof(LeftoversReserve),
                    nameof(WhiteStocks),
                    nameof(Transits),
                    nameof(Movements),
                    nameof(Suppliers),
                    nameof(Rrp),
                    nameof(Competitors),
                    nameof(PurchasePriceStats),
                    nameof(PurchaseHistory),
                    nameof(ContractorSalesHistory),
                    nameof(TelemartSalesHistory),
                    nameof(ViewsInfo),
                    nameof(HotlineCompetitorPrices),
                    nameof(Prices),
                    nameof(Showcases),
                    nameof(ViewsSummary));

                if (productId.TradeInProductInfo?.OrderId != null && productId.TradeInProductInfo.OrderStateId == OrderStatus.Done.Id)
                {
                    try
                    {
                        Result<TradeInPriceCalculatorResultDto> result = await WebClient.ExecuteApiRequestAsync(new QueryProductTradeInPrice(productId.TradeInProductInfo.OrderId.Value, productId.ProductId));
                        ServiceItems = MapServiceItems(result.Data).ToReadOnlyObservableCollection();
                    }
                    catch (Exception)
                    {
                    }
                }

                hotlineInfo = productInfo.Hotline;

                SetCurrency();
            }
            finally
            {
                IsLoadingInProgress = setLoadingTo;
            }
        }

        private IEnumerable<ProductMovementViewItem> MapMovements(IEnumerable<ProductInfoMovementDto> movements)
        {
            if (movements != null)
            {
                foreach (ProductInfoMovementDto movement in movements)
                {
                    warehouses.TryGetValue(movement.FromWarehouseId, out string w1);
                    warehouses.TryGetValue(movement.ToWarehouseId, out string w2);

                    string timeGet = movement.DateIn.ToString("HH:mm");

                    yield return new ProductMovementViewItem
                    {
                        MovementName = $"{w1} {w2} {timeGet}",
                        Available = movement.Available,
                        Overall = movement.Overall,
                        DateIn = movement.DateIn
                    };
                }
            }
        }

        private IEnumerable<ProductTransitViewItem> MapTransits(IEnumerable<ProductTransitDto> transits)
        {
            if (transits != null)
            {
                foreach (ProductTransitDto transit in transits)
                {
                    contractors.TryGetValue(transit.SupplierId, out string supplierName);

                    yield return new ProductTransitViewItem
                    {
                        TransitName = supplierName,
                        Available = transit.Available,
                        Overall = transit.Overall,
                        DateGet = transit.DateGet,
                        IgnoreTransit = transit.IgnoreTransit
                    };
                }
            }
        }

        private void ShowHotlineCompetitors()
        {
            SizeableDialogDocumentManagerService.ShowView<HotlineCompetitorsViewModel>(null, this);

            SetProduct();
        }

        private IEnumerable<SummaryViewItem> MapViewInfo(ProductInfoDto info)
        {
            yield return new SummaryViewItem("Продакт", employees.GetValueOrDefault(info.EmployeeId));
            yield return new SummaryViewItem("Закупщик", employees.GetValueOrDefault(info.EmployeeSupId));

            if (!string.IsNullOrWhiteSpace(info.PriceComment))
            {
                yield return new SummaryViewItem("Коммент.", info.PriceComment, SummaryViewItem.RedLevel);
            }
        }

        private void SetHotlineInfo()
        {
            HotlineInfo = MapHotlineInfo(hotlineInfo).ToReadOnlyObservableCollection();
        }

        private Task<ProductInfoDto> GetProductInfoAsync(int productId, int? contractorId)
        {
            return WebClient.ExecuteCatalogApiRequestAsync(new QueryProductInfo(productId, contractorId));
        }

        private void InitializeTimers()
        {
            requestProductTimer = new DispatcherTimer(DispatcherPriority.Background, Application.Current.Dispatcher);
            requestIntervalTimer = new DispatcherTimer(DispatcherPriority.Background, Application.Current.Dispatcher);

            requestProductTimer.Tick += RequestProductTimerTick;
            requestProductTimer.Interval = TimeSpan.FromMilliseconds(RequestProductTimerDelayMilliseconds);

            requestIntervalTimer.Tick += RequestIntervalTimerTick;
            requestIntervalTimer.Interval = TimeSpan.FromMilliseconds(RequestIntervalMilliseconds);
        }

        private void RequestIntervalTimerTick(object sender, EventArgs e)
        {
            requestIntervalTimer.Stop();
        }

        private void RequestProductTimerTick(object sender, EventArgs e)
        {
            SetProduct();
            requestProductTimer.Stop();
        }

        private void SetProduct()
        {
            if (ProductId != null)
            {
                SetProductAsync(ProductId).ContinueWith(
                    (t, state) =>
                    {
                        if (t.Exception != null)
                        {
                            Logger.LogError(t.Exception.InnerException, "Failed to refresh product information control");
                            MessageFacadeService.ShowNotificationError("Ошибка при загрузке данных");
                        }
                    },
                    TaskContinuationOptions.OnlyOnRanToCompletion,
                    TaskScheduler.FromCurrentSynchronizationContext());
            }
        }

        private void ShowProductInfo()
        {
            if (ProductId != null)
            {
                SizeableDialogDocumentManagerService.ShowView<ProductCardViewModel>(new ProductCardViewMessage(ProductId.ProductId), this);
            }
        }

        private void SetCurrencyMode(CurrencyMode mode)
        {
            SelectedCurrencyMode = mode;
            SetCurrency();
        }

        private void SetCurrency()
        {
            SelectedCurrency = SelectedCurrencyMode switch
            {
                CurrencyMode.Auto => Currency.GetById(ProductId.CurrencyId),
                CurrencyMode.Uah => Currency.Uah,
                CurrencyMode.Usd => Currency.Usd,
                CurrencyMode.Eur => Currency.Eur,
                _ => SelectedCurrency
            };

            SetDisplayPrices();
        }

        private void SetDisplayPrices()
        {
            Leftovers?.ForEach(x => x.DisplayPrice = GetFormattedPrice(x.PriceUah, x.PriceUsd));
            WhiteStocks?.ForEach(x => x.DisplayPrice = GetFormattedPrice(x.PriceUah, x.PriceUsd));
            Competitors?.ForEach(x => x.DisplayPrice = GetFormattedPrice(x.PriceUah, x.PriceUsd));
            Suppliers?.ForEach(x =>
            {
                x.DisplayPrice = GetSupplierFormattedPrice(x.SourcePriceUah, x.PriceUah, x.SourcePriceUsd, x.PriceUsd);

                if (SelectedCurrency == Currency.Uah)
                {
                    x.DisplayPriceToolTip = x.PriceUah.ToString(UahFormatter) == x.SourcePriceUah.ToString(UahFormatter) ? PriceToolTip : ContractorPricesToolTip;
                }
                else
                {
                    x.DisplayPriceToolTip = x.PriceUsd.ToString(UsdFormatter) == x.SourcePriceUsd.ToString(UsdFormatter) ? PriceToolTip : ContractorPricesToolTip;
                }
            });
            Rrp?.ForEach(x => x.DisplayPrice = GetFormattedPrice(x.PriceUah, x.PriceUsd));
            ContractorSalesHistory?.ForEach(x => x.DisplayPrice = GetFormattedPrice(x.PriceUah, x.PriceUsd));
            TelemartSalesHistory?.ForEach(x => x.DisplayPrice = GetFormattedPrice(x.PriceUah, x.PriceUsd));
            PurchaseHistory?.ForEach(x => x.DisplayPrice = GetFormattedPrice(x.PriceUah, x.PriceUsd));
            HotlineCompetitorPrices?.ForEach(x => x.DisplayPrice = GetFormattedPrice(x.PriceUah, x.PriceUsd));
            Prices?.ForEach(x => x.DisplayPrice = GetFormattedPrice(x.PriceUah, x.PriceUsd));
            PurchasePriceStats?.ForEach(x =>
            {
                x.DisplayMinPrice = GetFormattedPrice(x.MinPriceUah, x.MinPriceUsd);
                x.DisplayMaxPrice = GetFormattedPrice(x.MaxPriceUah, x.MaxPriceUsd);
                x.DisplayAveragePrice = GetFormattedPrice(x.AveragePriceUah, x.AveragePriceUsd);
            });

            SearchTemplates?.ForEach(
                x =>
                {
                    x.DisplayMinPrice = GetFormattedPrice(x.MinPriceUah, x.MinPriceUsd);
                    x.DisplayAvgPrice = GetFormattedPrice(x.AvgPriceUah, x.AvgPriceUsd);
                    x.DisplayMaxPrice = GetFormattedPrice(x.MaxPriceUah, x.MaxPriceUsd);
                });

            if (hotlineInfo != null)
            {
                hotlineInfo.DisplayPrice = GetFormattedPrice(hotlineInfo.PriceUah, hotlineInfo.PriceUsd);
                hotlineInfo.DisplayMinPrice = GetFormattedPrice(hotlineInfo.PriceMinUah, hotlineInfo.PriceMinUsd);

                SetHotlineInfo();
            }
        }

        private string GetFormattedPrice(decimal priceUah, decimal priceUsd)
        {
            return SelectedCurrency == Currency.Uah
                ? priceUah.ToString(UahFormatter)
                : priceUsd.ToString(UsdFormatter);
        }

        private string GetSupplierFormattedPrice(decimal sourcePriceUah, decimal priceUah, decimal sourcePriceUsd, decimal priceUsd)
        {
            if (SelectedCurrency == Currency.Uah)
            {
                return sourcePriceUah.ToString(UahFormatter) == priceUah.ToString(UahFormatter) ? priceUah.ToString(UahFormatter) : $"{sourcePriceUah.ToString(UahFormatter)}\n{priceUah.ToString(UahFormatter)}";
            }

            return sourcePriceUsd.ToString(UsdFormatter) == priceUsd.ToString(UsdFormatter) ? priceUsd.ToString(UsdFormatter) : $"{sourcePriceUsd.ToString(UsdFormatter)}\n{priceUsd.ToString(UsdFormatter)}";
        }

        private void ShowShowcase(int? id)
        {
            DialogDocumentManagerService.ShowView<ShowcaseViewModel>(new ShowcaseParameter(id ?? 0, ProductId?.ProductId), this);
        }

        private ProductShowcaseViewItem MapShowcase(ShowcaseDto source, ProductShowcaseViewItem target)
        {
            target.Id = source.Id;
            target.WarehouseName = warehouses[source.WarehouseId];
            target.Capacity = source.Capacity;
            target.Active = source.Active;

            return target;
        }

        private void OnShowcaseMessage(ShowcaseMessage message)
        {
            if (Showcases == null || message.Entity == null)
            {
                return;
            }

            switch (message.MessageType)
            {
                case MessageType.Added:
                    Showcases.Insert(0, MapShowcase(message.Entity, new ProductShowcaseViewItem()));
                    break;
                case MessageType.Changed:
                    Showcases.DoActionWithItem(x => x.Id == message.Entity.Id, x => MapShowcase(message.Entity, x));
                    break;
            }

            RaisePropertyChanged(nameof(ShowcasesVisible));
        }
    }
}