using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using DevExpress.Mvvm.UI;
using Microsoft.Extensions.Logging;
using Telemart.Client.Common.MvvmEnhancements;
using Telemart.Client.Common.Services;
using Telemart.Client.Data.Requests.Features.ServiceCenter;
using Telemart.Client.Data.Requests.Features.ServiceRepair;
using Telemart.Client.Data.Requests.Features.Warehouse;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Extensions;
using Telemart.Client.Properties;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.Paging;
using Telemart.Client.TransferObjects.Warehouse;
using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.Service.ServiceRepairs.FormInvoice
{
    public sealed class SearchRequestsViewModel :
        WizardPageViewModelBase<FormInvoiceModel>,
        ISupportWizardNextCommand,
        IFilteringItem
    {
        private readonly ILogger _logger;

        public SearchRequestsViewModel(
            IWebClient webClient,
            IMessageFacadeService messageFacadeService,
            IMapper mapper,
            ILogger<SearchRequestsViewModel> logger)
        {
            WebClient = webClient ?? throw new ArgumentNullException(nameof(webClient));
            MessageFacadeService = messageFacadeService ?? throw new ArgumentNullException(nameof(messageFacadeService));
            Mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));

            HandleLoadedCommand = new AsyncCommand(HandleLoadedAsync);

            _logger = logger;
        }

        #region Commands

        public IAsyncCommand HandleLoadedCommand { get; }

        #endregion

        #region Collections

        public ReadOnlyObservableCollection<ComboBoxItem> Warehouses
        {
            get { return GetProperty(() => Warehouses); }
            private set { SetProperty(() => Warehouses, value); }
        }

        public ReadOnlyObservableCollection<ComboBoxItem> ServiceCenters
        {
            get { return GetProperty(() => ServiceCenters); }
            private set { SetProperty(() => ServiceCenters, value); }
        }

        #endregion

        public bool CanGoForward => Model.ServiceCenterId.HasValue && !IsLongOperationInProgress;

        public override string Description { get; } = "Необходимо задать праметры для поиска ремонтов";

        public override string Header { get; } = "Шаг 1 - Выбор СЦ";

        private IWebClient WebClient { get; }

        private IMessageFacadeService MessageFacadeService { get; }

        private IMapper Mapper { get; }

        private CurrentWindowService CurrentWindowService => (CurrentWindowService)GetService<ICurrentWindowService>(ServiceSearchMode.PreferParents);

        private IWizardService WizardService => GetService<IWizardService>();

        public void OnGoForward(CancelEventArgs e)
        {
            CurrentWindowService.ActualWindow.Closing += ActualWindowClosing;

            IsLongOperationInProgress = true;
            RaisePropertyChanged(nameof(CanGoForward));

            try
            {
                Model.ServiceRepairs = null;

                QueryServiceRepairs request = new QueryServiceRepairs(this);

                PagedResult<ServiceRepairDto> serviceRepairs = WebClient.ExecuteApiRequest(request);

                List<ServiceRepairDto> requests = serviceRepairs.Data
                    .Where(x => x.ServiceRequestLocationId == Model.Location.Id
                        && x.ServiceRequestWarehouseLocationId == Model.WarehouseId
                        && x.ServiceCenterId.HasValue
                        && x.ServiceCenterId == Model.ServiceCenterId
                        && x.ServiceInvoiceId == null)
                    .ToList();

                if (requests.Any())
                {
                    Model.ServiceRepairs = requests.Select(x => Mapper.Map<ServiceRepairViewItem>(x)).ToObservableCollection();
                    WizardService.NavigateToView<SelectRequestsViewModel>(Model, ((ISupportParentViewModel)this).ParentViewModel);
                }
                else
                {
                    MessageFacadeService.ShowNotificationWarning("Нечего отгружать");
                }
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Error while getting repairs");
                MessageFacadeService.ShowNotificationError("Ошибка при получении ремонтов");
            }
            finally
            {
                IsLongOperationInProgress = false;
                RaisePropertyChanged(nameof(CanGoForward));

                CurrentWindowService.ActualWindow.Closing -= ActualWindowClosing;
            }
        }

        public IEnumerable<(string Name, object Value)> BuildParameters()
        {
            yield return ("states", Model.State.Id.ToString());
        }

        private async Task HandleLoadedAsync()
        {
            try
            {
                List<ServiceCenterDto> serviceCenters = await WebClient.ExecuteApiRequestAsync(new QueryServiceCenters(), true).GetPagedResultDataAsync();
                List<WarehouseDto> warehouses = await WebClient.ExecuteApiRequestAsync(new QueryWarehouses(), true).GetPagedResultDataAsync();

                ServiceCenters = serviceCenters
                    .Where(x => x.Active)
                    .OrderBy(x => x.Name)
                    .Select(x => new ComboBoxItem(x.Id, x.Name))
                    .ToReadOnlyObservableCollection();

                Warehouses = warehouses.Select(x => new ComboBoxItem(x.Id, x.Name)).ToReadOnlyObservableCollection();

                Model.SetInitialData();
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, Resources.ErrorDuringDataLoading);
                MessageFacadeService.ShowNotificationError(Resources.ErrorDuringDataLoading);
            }
        }

        private void ActualWindowClosing(object sender, CancelEventArgs e)
        {
            e.Cancel = true;
        }
    }
}