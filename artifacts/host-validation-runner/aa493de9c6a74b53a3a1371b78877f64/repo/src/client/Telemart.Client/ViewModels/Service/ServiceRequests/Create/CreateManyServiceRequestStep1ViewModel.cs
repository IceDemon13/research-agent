using System;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using DevExpress.Mvvm;
using DevExpress.Mvvm.UI;
using Microsoft.Extensions.Logging;
using Telemart.Client.Common.Services;
using Telemart.Client.Core.Exceptions;
using Telemart.Client.Data.Requests.Features.Order;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.Properties;
using Telemart.Client.TransferObjects;
using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.Service.ServiceRequests.Create
{
    public sealed class CreateManyServiceRequestStep1ViewModel : WizardPageViewModelBase<CreateManyServiceRequestModel>, ISupportWizardNextCommand
    {
        private readonly ILogger _logger;

        public CreateManyServiceRequestStep1ViewModel(
            IMessageFacadeService messageFacadeService,
            IWebClient webClient,
            IDictionaries dictionaries,
            ILogger<CreateManyServiceRequestStep1ViewModel> logger)
        {
            MessageFacadeService = messageFacadeService;
            WebClient = webClient;
            Dictionaries = dictionaries;

            HandleLoadedCommand = new AsyncCommand(HandleLoadedAsync);
            _logger = logger;
        }

        public CreateManyServiceRequestStep1ViewModel()
        {
        }

        public IAsyncCommand HandleLoadedCommand { get; }

        public bool CanGoForward { get; } = true;

        public override string Description { get; } = "Укажите номер заказа";

        public override string Header { get; } = "Поиск заказа";

        private IMessageFacadeService MessageFacadeService { get; }

        private IWebClient WebClient { get; }

        private IDictionaries Dictionaries { get; }

        private IWizardService WizardService => GetService<IWizardService>();

        private string[] ValidatableProperties { get; } =
        {
            nameof(CreateManyServiceRequestModel.SearchOrderId)
        };

        private CurrentWindowService CurrentWindowService => (CurrentWindowService)GetService<ICurrentWindowService>(ServiceSearchMode.PreferParents);

        public void OnGoForward(CancelEventArgs e)
        {
            Task<bool> task = OnGoForwardInternalAsync();

            task.ContinueWith(
                   t =>
                   {
                       if (t.Result)
                       {
                           WizardService.NavigateToView<CreateManyServiceRequestStep2ViewModel>(Model, ((ISupportParentViewModel)this).ParentViewModel);
                       }

                       e.Cancel = true;
                   },
                   TaskScheduler.FromCurrentSynchronizationContext());
        }

        protected override bool GetCanCancel()
        {
            return !IsLongOperationInProgress;
        }

        private Task HandleLoadedAsync()
        {
            Model.ClearOrderData();
            return Task.CompletedTask;
        }

        private async Task<bool> OnGoForwardInternalAsync()
        {
            if (IDataErrorInfoHelper.HasErrors(Model, false, propertyFilter: x => ValidatableProperties.Contains(x.Name)))
            {
                return false;
            }

            bool success = false;

            CurrentWindowService.ActualWindow.Closing += ActualWindowClosing;

            IsLongOperationInProgress = true;

            try
            {
                success = await SetOrderAsync(Model.SearchOrderId.Value);
            }
            catch (UnexpectedSatusException exception) when (exception.Args.HttpStatusCode == HttpStatusCode.Unauthorized)
            {
                MessageFacadeService.ShowNotificationError("Не хватает прав для выполнения операции");
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, Resources.ErrorDuringDataLoading);
                MessageFacadeService.ShowNotificationError(Resources.ErrorDuringDataLoading);
            }
            finally
            {
                IsLongOperationInProgress = false;
                CurrentWindowService.ActualWindow.Closing -= ActualWindowClosing;
            }

            return success;
        }

        private async Task<bool> SetOrderAsync(int orderId)
        {
            bool success = false;

            try
            {
                OrderDto order = await WebClient.ExecuteApiRequestAsync(new QueryOrder(orderId));

                await Model.SetOrderDataAsync(Dictionaries, order);

                success = true;
            }
            catch (UnexpectedSatusException exception) when (exception.Args.HttpStatusCode == HttpStatusCode.NotFound)
            {
                MessageFacadeService.ShowNotificationError($"Заказ №{orderId.ToString(CultureInfo.InvariantCulture)} не найден");
            }

            return success;
        }

        private void ActualWindowClosing(object sender, CancelEventArgs e)
        {
            e.Cancel = true;
        }
    }
}