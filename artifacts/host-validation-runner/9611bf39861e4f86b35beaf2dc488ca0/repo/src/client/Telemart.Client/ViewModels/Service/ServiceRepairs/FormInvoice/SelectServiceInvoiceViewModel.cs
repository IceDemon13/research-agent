using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using DevExpress.Mvvm.UI;
using Microsoft.Extensions.Logging;
using Telemart.Client.Common.Messages;
using Telemart.Client.Common.Services;
using Telemart.Client.Core.Exceptions;
using Telemart.Client.Data.Requests.Features.ServiceInvoice;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Extensions;
using Telemart.Client.Properties;
using Telemart.Client.TransferObjects;
using Telemart.Client.ViewModels.Base;
using Telemart.Client.ViewModels.Service.ServiceInvoices;
using Telemart.Client.ViewModels.Validation;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.ViewModels.Service.ServiceRepairs.FormInvoice
{
    public sealed class SelectServiceInvoiceViewModel :
        WizardPageViewModelBase<FormInvoiceModel>,
        ISupportWizardBackCommand,
        ISupportWizardNextCommand
    {
        public SelectServiceInvoiceViewModel(
            IWebClient webClient,
            IMessageFacadeService messageFacadeService,
            IMapper mapper,
            IMessenger messenger,
            ILogger<SelectServiceInvoiceViewModel> logger)
        {
            WebClient = webClient ?? throw new ArgumentNullException(nameof(webClient));
            MessageFacadeService = messageFacadeService ?? throw new ArgumentNullException(nameof(messageFacadeService));
            Mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            Messenger = messenger ?? throw new ArgumentNullException(nameof(messenger));
            Logger = logger;

            HandleLoadedCommand = new AsyncCommand(HandleLoadedAsync);
            CreateServiceInvoiceCommand = new AsyncCommand(CreateServiceInvoiceAsync);
        }

        #region Commands

        public IAsyncCommand HandleLoadedCommand { get; }

        public IAsyncCommand CreateServiceInvoiceCommand { get; }

        #endregion

        #region Collections

        #endregion

        public bool CanGoForward => Model.SelectedServiceInvoice != null && Model.SelectedServiceInvoice.Id > 0 && !IsLongOperationInProgress;

        public bool CanGoBack => !IsLongOperationInProgress;

        public override string Description { get; } = "Выберите серв. накладную";

        public override string Header { get; } = "Шаг 3 - Выбор накладной";

        private IWebClient WebClient { get; }

        private IMessageFacadeService MessageFacadeService { get; }

        private IMessenger Messenger { get; }

        private IMapper Mapper { get; }

        private ILogger<SelectServiceInvoiceViewModel> Logger { get; }

        private IWizardService WizardService => GetService<IWizardService>();

        private CurrentWindowService CurrentWindowService => (CurrentWindowService)GetService<ICurrentWindowService>(ServiceSearchMode.PreferParents);

        private IDocumentManagerService DialogDocumentManagerService => GetService<IDocumentManagerService>("DialogDocumentManagerService", ServiceSearchMode.PreferParents);

        public void OnGoBack(CancelEventArgs e)
        {
            Model.ServiceInvoices = null;
        }

        public void OnGoForward(CancelEventArgs e)
        {
            OnGoForwardAsync().ContinueWith(
                _ => WizardService.NavigateToView<FinishFormInvoiceViewModel>(Model, ((ISupportParentViewModel)this).ParentViewModel),
                TaskScheduler.FromCurrentSynchronizationContext());
        }

        private async Task OnGoForwardAsync()
        {
            CurrentWindowService.ActualWindow.Closing += ActualWindowClosing;

            Model.ValidationItems = null;

            IsLongOperationInProgress = true;

            try
            {
                FormServiceInvoiceRequest gatewayRequest = new FormServiceInvoiceRequest(
                    Model.SelectedServiceInvoice.Id,
                    Model.SelectedServiceRepair.Select(x => x.Id).ToArray());

                Result<ServiceInvoiceDto> result = await WebClient.ExecuteApiRequestAsync(gatewayRequest);

                if (result.Warnings?.Any() == true)
                {
                    Model.ValidationItems = result.Warnings.Select(x => new ValidationResultItem(x, false)).ToObservableCollection();
                }

                Model.Result = result;

                Messenger.Send(new ServiceInvoiceMessage(result.Data, MessageType.Changed));
            }
            catch (UnexpectedSatusException exception)
            {
                Model.ValidationItems = new ObservableCollection<ValidationResultItem>(exception.GetErrorItems());
            }
            catch (UnexpectedErrorException exception)
            {
                Logger.LogError(exception, "Failed to form service invoice");

                Model.ValidationItems = new ObservableCollection<ValidationResultItem> { new ValidationResultItem(Resources.ServerUnavailable, true) };
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Failed to form service invoice");

                Model.ValidationItems = new ObservableCollection<ValidationResultItem> { new ValidationResultItem("Непредвиденная ошибка", true) };
            }
            finally
            {
                IsLongOperationInProgress = false;

                CurrentWindowService.ActualWindow.Closing -= ActualWindowClosing;
            }
        }

        private Task HandleLoadedAsync()
        {
            return Task.CompletedTask;
        }

        private Task CreateServiceInvoiceAsync()
        {
            IsLongOperationInProgress = true;

            try
            {
                CreateServiceInvoiceParameter parameter = new CreateServiceInvoiceParameter(Model.ServiceCenterId.Value, Model.WarehouseId);

                CreateServiceInvoiceViewModel viewModel = DialogDocumentManagerService.ShowView<CreateServiceInvoiceViewModel>(parameter, this);

                if (viewModel.IsOk)
                {
                    ServiceInvoiceViewItem viewItem = Mapper.Map<ServiceInvoiceViewItem>(viewModel.ResultServiceInvoice);

                    Model.ServiceInvoices.Add(viewItem);

                    MessageFacadeService.ShowNotificationInfo($"Сервисная накладная №{viewModel.ResultServiceInvoice.Id} успешно создана");
                }
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, Resources.ErrorDuringDataLoading);
                MessageFacadeService.ShowNotificationError(Resources.ErrorDuringDataLoading);
            }
            finally
            {
                IsLongOperationInProgress = false;
            }

            return Task.CompletedTask;
        }

        private void ActualWindowClosing(object sender, CancelEventArgs e)
        {
            e.Cancel = true;
        }
    }
}