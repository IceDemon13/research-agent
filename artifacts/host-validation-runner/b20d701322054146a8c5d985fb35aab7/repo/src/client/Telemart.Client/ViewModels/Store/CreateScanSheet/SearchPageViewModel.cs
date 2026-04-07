using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using DynamicData;
using Microsoft.Extensions.Logging;
using Telemart.Client.Common.MvvmEnhancements;
using Telemart.Client.Common.Services;
using Telemart.Client.Data.Requests.Features.Novaposhta;
using Telemart.Client.Data.Requests.Features.Order;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.Novaposhta;
using Telemart.Client.TransferObjects.Paging;
using Telemart.Client.ViewModels.Base;
using Telemart.Client.ViewModels.Novaposhta;

namespace Telemart.Client.ViewModels.Store.CreateScanSheet
{
    public sealed class SearchPageViewModel : WizardPageViewModelBase<CreateScanSheetModel>, ISupportWizardNextCommand
    {
        private readonly ILogger _logger;

        public SearchPageViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            IMapper mapper,
            ILogger<SearchPageViewModel> logger)
        {
            WebClient = webClient ?? throw new ArgumentNullException(nameof(webClient));
            Dictionaries = dictionaries ?? throw new ArgumentNullException(nameof(dictionaries));
            MessageFacadeService = messageFacadeService ?? throw new ArgumentNullException(nameof(messageFacadeService));
            Mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));

            HandleLoadedCommand = new AsyncCommand(HandleLoadedAsync);

            _logger = logger;
        }

        public SearchPageViewModel()
        {
        }

        #region Commands

        public IAsyncCommand HandleLoadedCommand { get; }

        #endregion

        public bool CanGoForward => Model.ScanSheetProcessorType.HasValue
            && Model.SelectedCarries?.Any() == true
            && !IsLongOperationInProgress;

        public ReadOnlyObservableCollection<ComboBoxItem> CourierCalls
        {
            get { return GetProperty(() => CourierCalls); }
            private set { SetProperty(() => CourierCalls, value); }
        }

        public DateTime DateFromMinDate { get; } = DateTime.Today.AddDays(-14);

        public DateTime DateFromMaxDate { get; } = DateTime.Today.AddDays(1).AddSeconds(-1);

        public DateTime DateToMinDate { get; } = DateTime.Today;

        public DateTime DateToMaxDate { get; } = DateTime.Today.AddDays(1).AddSeconds(-1);

        public override string Description { get; } = "Необходимо задать праметры для поиска заказов";

        public override string Header { get; } = "Шаг 1 - Подбор параметров поиска заказов";

        private IDictionaries Dictionaries { get; }

        private IWebClient WebClient { get; }

        private IMessageFacadeService MessageFacadeService { get; }

        private IMapper Mapper { get; }

        private IWizardService WizardService => GetService<IWizardService>();

        public void OnGoForward(CancelEventArgs e)
        {
            IsLongOperationInProgress = true;

            Model.Orders = null;
            Model.PlacesCount = 0;

            try
            {
                QueryOrders request = new QueryOrders(GetOrderFilteringItem());
                PagedResult<OrderDto> pagedResult = WebClient.ExecuteApiRequest(request);

                if (Model.ScanSheetProcessorType == Business.Delivery.ScanSheets.ScanSheetProcessorType.Novaposhta && Model.SelectedCourierCall.HasValue)
                {
                    pagedResult.Data = pagedResult.Data.Where(x => x.NpCourierCallBarcode == Model.SelectedCourierCall.Value.Ref).ToList();
                }

                if (pagedResult.Data.Any())
                {
                    Model.Orders = pagedResult.Data.Select(x => Mapper.Map<ScanSheetOrderViewItem>(x)).ToObservableCollection();
                    Model.PlacesCount = pagedResult.Data.Sum(x => x.PackagePlaces);

                    WizardService.NavigateToView<SelectPageViewModel>(Model, ((ISupportParentViewModel)this).ParentViewModel);
                }
                else
                {
                    e.Cancel = true;
                    MessageFacadeService.ShowNotificationWarning("Нечего отгружать");
                }
            }
            catch (Exception exception)
            {
                e.Cancel = true;
                MessageFacadeService.ShowNotificationError("Ошибка при получении заказов");
                _logger.LogError(exception, "Failed to get prder");
            }
            finally
            {
                IsLongOperationInProgress = false;
                RaisePropertyChanged(nameof(CanGoForward));
            }
        }

        private OrderFilteringItem GetOrderFilteringItem()
        {
            OrderFilteringItem filteringItem = new OrderFilteringItem(null, new List<int>());

            filteringItem.Carries = Model.SelectedCarries.Select(x => x.Id).ToList();
            filteringItem.Warehouses = new List<int> { Model.SelectedWarehouse.Id };
            filteringItem.OrderStatuses = new List<int> { Model.SelectedState.Id };
            filteringItem.OrderClosedAfter = Model.DateFrom;
            filteringItem.OrderClosedBefore = Model.DateTo;

            return filteringItem;
        }

        private async Task HandleLoadedAsync()
        {
            Model.CompleteCourierCall = true;

            CourierCallFilteringItem filteringItem = new CourierCallFilteringItem
            {
                WarehouseId = Model.SelectedWarehouse.Id,
                Completed = false,
                Ttns = "*",
                From = DateTime.Today
            };

            List<NpCourierCallSimpleDto> courierCalls = await WebClient.ExecuteApiRequestAsync(new QueryNpCourierCalls(filteringItem));

            int id = 0;

            var comboBoxItems = courierCalls
                .OrderBy(x => x.Date.Date)
                    .ThenBy(x => x.TimeFrom)
                .Select(x => new ComboBoxItem(id++, string.Format("{0} {1} {2}-{3}", x.Barcode, x.Date.ToString("dd.MM"), x.TimeFrom.ToString("HH:mm"), x.TimeTo.ToString("HH:mm")), reference: x.Barcode))
                .ToList();
            comboBoxItems.Add(new ComboBoxItem(id++, "Без заявки"));

            CourierCalls = comboBoxItems.ToReadOnlyObservableCollection();

            Model.SelectedCourierCall = CourierCalls.First();
        }
    }
}