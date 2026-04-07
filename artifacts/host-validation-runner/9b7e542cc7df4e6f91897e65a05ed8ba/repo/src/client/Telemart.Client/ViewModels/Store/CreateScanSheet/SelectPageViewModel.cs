using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO.Abstractions;
using System.Linq;
using System.Threading.Tasks;
using DevExpress.Mvvm;
using DevExpress.Mvvm.UI;
using MediatR;
using Microsoft.Extensions.Logging;
using Telemart.Client.Business.Delivery;
using Telemart.Client.Business.Delivery.ScanSheets;
using Telemart.Client.Common.ErrorHandler;
using Telemart.Client.Common.Services;
using Telemart.Client.Common.Settings.Printing;
using Telemart.Client.Core.Exceptions;
using Telemart.Client.Data.Requests;
using Telemart.Client.Data.Requests.Features.Carry;
using Telemart.Client.Data.Requests.Features.Order;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Extensions;
using Telemart.Client.Properties;
using Telemart.Client.TransferObjects;
using Telemart.Client.ViewModels.Base;
using Telemart.Client.ViewModels.Validation;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.ViewModels.Store.CreateScanSheet
{
    public sealed class SelectPageViewModel :
        WizardPageViewModelBase<CreateScanSheetModel>,
        ISupportWizardNextCommand,
        ISupportWizardBackCommand
    {
        public SelectPageViewModel(
            IWebClient webClient,
            IMediator mediator,
            IPrintingSettingsStore printingSettingsStore,
            IFileSystem fileSystem,
            ILogger<SelectPageViewModel> logger,
            IMessageFacadeService messageFacadeService,
            IErrorHandler errorHandler)
        {
            WebClient = webClient;
            Mediator = mediator;
            PrintingSettingsStore = printingSettingsStore;
            FileSystem = fileSystem;
            Logger = logger;
            MessageFacadeService = messageFacadeService;
            ErrorHandler = errorHandler;
        }

        public bool CanGoBack => !IsLongOperationInProgress;

        public bool CanGoForward => !IsLongOperationInProgress;

        public override string Description { get; } = "Эти заказы попадут в создаваемый реестр";

        public override string Header { get; } = "Шаг 2 - Просмотр заказов";

        private IMediator Mediator { get; }

        private IMessageFacadeService MessageFacadeService { get; }

        private IPrintingSettingsStore PrintingSettingsStore { get; }

        private IFileSystem FileSystem { get; }

        private IWebClient WebClient { get; }

        private ILogger<SelectPageViewModel> Logger { get; }

        private IWizardService WizardService => GetService<IWizardService>();

        private IErrorHandler ErrorHandler { get; }

        private CurrentWindowService CurrentWindowService => (CurrentWindowService)GetService<ICurrentWindowService>(ServiceSearchMode.PreferParents);

        public void OnGoBack(CancelEventArgs e)
        {
        }

        public void OnGoForward(CancelEventArgs e)
        {
            OnGoForwardAsync().ContinueWith(
                _ => WizardService.NavigateToView<FinishPageViewModel>(Model, ((ISupportParentViewModel)this).ParentViewModel),
                TaskScheduler.FromCurrentSynchronizationContext());
        }

        protected override bool GetCanCancel()
        {
            return !IsLongOperationInProgress;
        }

        private static string GetProgressText(int progressPercentage)
        {
            return $"Обработано {progressPercentage}%";
        }

        private void ActualWindowClosing(object sender, CancelEventArgs e)
        {
            e.Cancel = true;
        }

        private async Task OnGoForwardAsync()
        {
            CurrentWindowService.ActualWindow.Closing += ActualWindowClosing;

            Model.ValidationItems = null;

            Model.ProgressText = "Создаем реестр";
            Model.ProgressValue = 0;

            IsLongOperationInProgress = true;

            int[] orderIds = Model.Orders.Select(x => x.Id).ToArray();

            try
            {
                IScanSheetProcessor processor = Create(Model.ScanSheetProcessorType.Value);

                bool valid = processor.Validate(Model);

                if (valid)
                {
                    bool created = await CreateScanSheetAsync(processor, orderIds);

                    if (created)
                    {
                        Model.ProgressText = GetProgressText(0);

                        IProgress<ProgressInfo> progress = new Progress<ProgressInfo>(x =>
                        {
                            Model.ProgressText = x.Text;
                            Model.ProgressValue = x.Value;
                        });

                        ObservableCollection<ValidationResultItem> validationItems = await CreateOrdersRtAsync(orderIds, progress);

                        Model.ValidationItems = validationItems.Any()
                            ? validationItems
                            : null;

                        await processor.PrintAsync(Model);
                    }
                }
            }
            finally
            {
                IsLongOperationInProgress = false;

                CurrentWindowService.ActualWindow.Closing -= ActualWindowClosing;
            }
        }

        private async Task<bool> CreateScanSheetAsync(IScanSheetProcessor scanSheetProcessor, int[] orderIds)
        {
            bool ok = false;

            try
            {
                IRestClientGatewayRequest<Result<ScanSheetCreateResponse[]>> gatewayRequest = scanSheetProcessor.CreateRequest(orderIds);

                if (scanSheetProcessor is NovaposhtaScanSheetProcessor && gatewayRequest is CreateScanSheetRequest npRequest && Model.SelectedCourierCall?.Ref != null)
                {
                    (npRequest.Body as ScanSheetCreateRequest).CompleteCourierCallApplication = Model.CompleteCourierCall;
                }

                if (gatewayRequest != null)
                {
                    Result<ScanSheetCreateResponse[]> result = await WebClient.ExecuteApiRequestAsync(gatewayRequest);
                    Model.Result = result.Data;
                }
                else
                {
                    Model.Result = Array.Empty<ScanSheetCreateResponse>();
                }

                ok = true;
            }
            catch (UnexpectedSatusException exception)
            {
                Model.ValidationItems = new ObservableCollection<ValidationResultItem>(exception.GetErrorItems());
            }
            catch (UnexpectedErrorException exception)
            {
                Logger.LogError(exception, "Failed to create novaposhta scan sheet");

                Model.ValidationItems = new ObservableCollection<ValidationResultItem> { new ValidationResultItem(Resources.ServerUnavailable, true) };
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Failed to create novaposhta scan sheet");

                Model.ValidationItems = new ObservableCollection<ValidationResultItem> { new ValidationResultItem("Непредвиденная ошибка", true) };
            }

            return ok;
        }

        private async Task<ObservableCollection<ValidationResultItem>> CreateOrdersRtAsync(int[] orderIds, IProgress<ProgressInfo> progress)
        {
            const int OrdersPerRequest = 10;
            const int RetryCount = 3;
            const int RetryTimeoutSeconds = 5;

            int processed = 0;
            double total = orderIds.Length * RetryCount;

            HashSet<int> successOrderIds = new HashSet<int>();

            ObservableCollection<ValidationResultItem> validationItems = new ObservableCollection<ValidationResultItem>();

            for (int i = 0; i < RetryCount; i++)
            {
                validationItems.Clear();

                int[] orderIdsToProcess = orderIds.Where(orderId => !successOrderIds.Contains(orderId)).ToArray();

                foreach (IReadOnlyCollection<int> ids in orderIdsToProcess.Section(OrdersPerRequest))
                {
                    try
                    {
                        Result<OrdersCreateRtResponse> response = await ErrorHandler.HandleErrorsAsync(
                            _ => WebClient.ExecuteApiRequestAsync(new RtOrders(ids)),
                            "при создании реестра",
                            "Доставка сохранена",
                            this,
                            true,
                            showNotification: true);

                        foreach (OrderCreateRtResult orderCreateRtResult in response.Data.Data)
                        {
                            if (string.IsNullOrWhiteSpace(orderCreateRtResult.Error))
                            {
                                processed += RetryCount - i;
                                successOrderIds.Add(orderCreateRtResult.OrderId);
                            }
                            else
                            {
                                processed += 1;
                                validationItems.Add(new ValidationResultItem(orderCreateRtResult.Error, true));
                            }
                        }
                    }
                    catch (Exception exception)
                    {
                        processed += ids.Count;

                        validationItems.Add(new ValidationResultItem($"Ошибка при обработке заказов: {string.Join(",", ids.Select(x => x.ToString()))}", true));

                        Logger.LogError(exception, "Failed to create orders RT");
                    }

                    int progressPercentage = (int)Math.Truncate(processed * 100 / total);

                    progress?.Report(new ProgressInfo(GetProgressText(progressPercentage), progressPercentage));
                }

                if (validationItems.Any())
                {
                    await Task.Delay(RetryTimeoutSeconds * 1000);
                }
                else
                {
                    break;
                }
            }

            return validationItems;
        }

        private IScanSheetProcessor Create(ScanSheetProcessorType processorType)
        {
            IScanSheetProcessor processor;

            switch (processorType)
            {
                case ScanSheetProcessorType.Novaposhta:
                    processor = new NovaposhtaScanSheetProcessor(Mediator);
                    break;
                case ScanSheetProcessorType.Ukrposhta:
                    processor = new UkrposhtaScanSheetProcessor(Mediator);
                    break;
                case ScanSheetProcessorType.Telemart:
                    processor = new TelemartScanSheetProcessor(Mediator, MessageFacadeService, PrintingSettingsStore, FileSystem);
                    break;
                case ScanSheetProcessorType.MeestExpress:
                    processor = new MeestExpressScanSheetProcessor(Mediator);
                    break;
                default:
                    throw new NotSupportedException();
            }

            return processor;
        }

        private class ProgressInfo
        {
            public ProgressInfo(string text, int value)
            {
                Text = text;
                Value = value;
            }

            public string Text { get; }

            public int Value { get; }
        }
    }
}