using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using Microsoft.Extensions.Logging;
using Telemart.Client.Common.MvvmEnhancements;
using Telemart.Client.Common.Services;
using Telemart.Client.Core;
using Telemart.Client.Data.Requests.Features.Order;
using Telemart.Client.Data.Requests.Features.Warehouse;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.TransferObjects.Novaposhta;
using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.Store.CreateScanSheetForEntities
{
    public sealed class SearchCriteriaPageViewModel : WizardPageViewModelBase<CreateScanSheetForEntitiesModel>, ISupportWizardNextCommand
    {
        private readonly ILogger _logger;

        public SearchCriteriaPageViewModel()
        {
        }

        public SearchCriteriaPageViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            IMapper mapper,
            ILogger<SearchCriteriaPageViewModel> logger)
        {
            WebClient = webClient ?? throw new ArgumentNullException(nameof(webClient));
            Dictionaries = dictionaries ?? throw new ArgumentNullException(nameof(dictionaries));
            MessageFacadeService = messageFacadeService ?? throw new ArgumentNullException(nameof(messageFacadeService));
            Mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));

            HandleLoadedCommand = new AsyncCommand(HandleLoadedAsync);

            _logger = logger;
        }

        #region Commands

        public IAsyncCommand HandleLoadedCommand { get; }

        #endregion

        public ReadOnlyObservableCollection<ComboBoxItem> Warehouses
        {
            get { return GetProperty(() => Warehouses); }
            private set { SetProperty(() => Warehouses, value); }
        }

        public bool CanGoForward => Model.SelectedWarehouse.HasValue
            && Model.DateFrom.HasValue
            && Model.DateTo.HasValue
            && !IsLongOperationInProgress;

        public DateTime DateFromMinDate { get; } = DateTime.Today.AddDays(-14);

        public DateTime DateFromMaxDate { get; } = DateTime.Today.AddDays(1).AddSeconds(-1);

        public DateTime DateToMinDate { get; } = DateTime.Today;

        public DateTime DateToMaxDate { get; } = DateTime.Today.AddDays(1);

        public override string Description { get; } = "Необходимо задать праметры для поиска документов";

        public override string Header { get; } = "Шаг 1 - Подбор параметров поиска документов";

        private IDictionaries Dictionaries { get; }

        private IWebClient WebClient { get; }

        private IMessageFacadeService MessageFacadeService { get; }

        private IMapper Mapper { get; }

        private IWizardService WizardService => GetService<IWizardService>();

        public void OnGoForward(CancelEventArgs e)
        {
            IsLongOperationInProgress = true;

            Model.Entities = null;

            try
            {
                QueryNpDocuments request = new QueryNpDocuments(GetFilteringItem());

                IReadOnlyCollection<NpDocumentDto> pagedResult = AsyncHelper.RunSync(() => WebClient.ExecuteApiRequestAsync(request).GetPagedResultDataAsync());

                if (pagedResult.Any())
                {
                    Model.Entities = pagedResult.Select(Mapper.Map<ScanSheetEntityViewItem>).ToObservableCollection();

                    WizardService.NavigateToView<SelectEntitiesPageViewModel>(Model, ((ISupportParentViewModel)this).ParentViewModel);
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
                MessageFacadeService.ShowNotificationError("Ошибка при получении документов");
                _logger.LogError(exception, "Failed to get documents");
            }
            finally
            {
                IsLongOperationInProgress = false;
                RaisePropertyChanged(nameof(CanGoForward));
            }
        }

        private NpDocumentFilteringItem GetFilteringItem()
        {
            NpDocumentFilteringItem filteringItem = new NpDocumentFilteringItem
            {
                WarehouseId = Model.SelectedWarehouse.Value.Id,
                StatusCodes = "null,1",
                EntityTypeId = Model.EntityTypeId,
                CreatedAfter = Model.DateFrom,
                CreatedBefore = Model.DateTo,
                ScanSheet = false
            };

            return filteringItem;
        }

        private async Task HandleLoadedAsync()
        {
            var warehouses = await WebClient.ExecuteApiRequestAsync(new QueryWarehouses(), true).GetPagedResultDataAsync();

            Warehouses = warehouses
                .Where(x => x.Active == 1 && WebClient.AuthenticatedEmployee.AllowWarehouses.Contains(x.Id))
                .Select(x => new ComboBoxItem(x.Id, x.Name))
                .ToReadOnlyObservableCollection();

            if (Warehouses?.Any() != true)
            {
                MessageFacadeService.ShowNotificationWarning("У вас нет доступных складов");
            }

            Model.DateFrom = DateToMinDate;
            Model.DateTo = DateToMaxDate;
        }
    }
}