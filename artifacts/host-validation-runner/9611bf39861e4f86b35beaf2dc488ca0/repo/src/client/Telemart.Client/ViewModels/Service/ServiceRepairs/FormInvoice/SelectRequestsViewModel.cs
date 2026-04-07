using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using DevExpress.Mvvm.UI;
using Microsoft.Extensions.Logging;
using Telemart.Client.Common.Services;
using Telemart.Client.Data.Requests.Features.ServiceInvoice;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.Properties;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.Paging;
using Telemart.Client.ViewModels.Base;
using Telemart.Client.ViewModels.Service.ServiceInvoices;

namespace Telemart.Client.ViewModels.Service.ServiceRepairs.FormInvoice
{
    public sealed class SelectRequestsViewModel :
        WizardPageViewModelBase<FormInvoiceModel>,
        ISupportWizardBackCommand,
        ISupportWizardNextCommand
    {
        private readonly ILogger _logger;

        public SelectRequestsViewModel(IWebClient webClient, IMapper mapper, IMessageFacadeService messageFacadeService, ILogger<SelectRequestsViewModel> logger)
        {
            WebClient = webClient ?? throw new ArgumentNullException(nameof(webClient));
            Mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            MessageFacadeService = messageFacadeService ?? throw new ArgumentNullException(nameof(messageFacadeService));

            HandleLoadedCommand = new AsyncCommand(HandleLoadedAsync);
            _logger = logger;
        }

        #region Commands

        public IAsyncCommand HandleLoadedCommand { get; }

        #endregion

        public bool CanGoForward => !IsLongOperationInProgress;

        public bool CanGoBack => !IsLongOperationInProgress;

        public override string Description { get; } = "Выберите ремонты";

        public override string Header { get; } = "Шаг 2 - Выбор ремонтов";

        private IMapper Mapper { get; }

        private IMessageFacadeService MessageFacadeService { get; }

        private IWebClient WebClient { get; }

        private IWizardService WizardService => GetService<IWizardService>();

        private CurrentWindowService CurrentWindowService => (CurrentWindowService)GetService<ICurrentWindowService>(ServiceSearchMode.PreferParents);

        public void OnGoForward(CancelEventArgs e)
        {
            if (Model.SelectedServiceRepair == null || Model.SelectedServiceRepair.Count == 0)
            {
                MessageFacadeService.ShowNotificationWarning("Выберите хоть одну заявку");
                e.Cancel = true;
                return;
            }

            CurrentWindowService.ActualWindow.Closing += ActualWindowClosing;

            IsLongOperationInProgress = true;
            RaisePropertyChanged(nameof(CanGoForward));

            try
            {
                IFilteringItem filter = new ServiceInvoiceFilteringItem
                {
                    ServiceCenters = new List<int> { Model.ServiceCenterId.Value },
                    Warehouses = new List<int> { Model.WarehouseId },
                    States = new List<int> { ServiceInvoiceState.NewId, ServiceInvoiceState.ClosedId }
                };

                QueryServiceInvoices gatewayRequest = new QueryServiceInvoices(filter);

                PagedResult<ServiceInvoiceDto> serviceInvoices = WebClient.ExecuteApiRequest(gatewayRequest);

                Model.ServiceInvoices = serviceInvoices.Data.Select(x => Mapper.Map<ServiceInvoiceViewItem>(x)).ToObservableCollection();
                Model.ServiceInvoices.Insert(0, new ServiceInvoiceViewItem { Id = 0 });

                WizardService.NavigateToView<SelectServiceInvoiceViewModel>(Model, ((ISupportParentViewModel)this).ParentViewModel);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, Resources.ErrorDuringDataLoading);
                MessageFacadeService.ShowNotificationError(Resources.ErrorDuringDataLoading);
            }
            finally
            {
                IsLongOperationInProgress = false;
                RaisePropertyChanged(nameof(CanGoForward));
                CurrentWindowService.ActualWindow.Closing -= ActualWindowClosing;
            }
        }

        public void OnGoBack(CancelEventArgs e)
        {
            Model.ServiceRepairs = null;
        }

        private Task HandleLoadedAsync()
        {
            return Task.CompletedTask;
        }

        private void ActualWindowClosing(object sender, CancelEventArgs e)
        {
            e.Cancel = true;
        }
    }
}