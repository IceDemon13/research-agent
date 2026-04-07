using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using DevExpress.Mvvm;
using MediatR;
using Telemart.Client.Common.ErrorHandler;
using Telemart.Client.Common.Messages;
using Telemart.Client.Common.PrintSticker;
using Telemart.Client.Common.Services;
using Telemart.Client.Common.Settings.Printing;
using Telemart.Client.Core.Exceptions;
using Telemart.Client.Data.Requests.Features.Backlog.Actions;
using Telemart.Client.Data.Requests.Features.CompanyStructure;
using Telemart.Client.Data.Requests.Features.Employee;
using Telemart.Client.Data.Requests.Features.Invoice.Actions;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Data.WebClient.Security;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.Mediator.Requests;
using Telemart.Client.Reports;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.Backlog;
using Telemart.Client.TransferObjects.CompanyStructure;
using Telemart.Client.ViewModels.Backlog;
using Telemart.Client.ViewModels.Dialogs.Call;
using Telemart.Client.ViewModels.TradeIn;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.Common
{
    public sealed class DocumentCommands
    {
        private IReadOnlyDictionary<int, EmployeeDto> _employees;
        private IReadOnlyDictionary<int, HashSet<int>> _headEmployeeDepartments;
        private ISupportServices _supportServices;
        private IDocumentManagerService _dialogDocumentManagerService;

        public DocumentCommands(
            IMessageFacadeService messageFacadeService,
            IMessenger messenger,
            IWebClient webClient,
            IDictionaries dictionaries,
            IPrintingSettingsStore printingSettingsStore,
            IErrorHandler errorHandler,
            IMediator mediator)
        {
            MessageFacadeService = messageFacadeService;
            Messenger = messenger;
            WebClient = webClient;
            Dictionaries = dictionaries;
            PrintingSettingsStore = printingSettingsStore;
            Mediator = mediator;
            ErrorHandler = errorHandler;

            ShowSerialNumberHistoryCommand = new DelegateCommand<string>(ShowSerialNumberHistory);
            ShowProductCardCommand = new DelegateCommand<int?>(ShowProductCard, x => x.HasValue);
            ShowOrderCommand = new DelegateCommand<int?>(ShowOrder, x => x.HasValue);
            ShowCreateReturnInvoiceCommand = new AsyncCommand<int?>(ShowCreateReturnInvoiceAsync, x => x.HasValue);
            ShowServiceRequestCommand = new DelegateCommand<int?>(ShowServiceRequest, x => x.HasValue);
            ShowTradeInCommand = new DelegateCommand<int?>(ShowTradeIn, x => x.HasValue);
            ShowPhoneHistoryCommand = new DelegateCommand<object>(ShowPhoneHistory, CanShowPhoneHistory);
            ShowCallDialogCommand = new DelegateCommand<CallDialogParameter>(ShowCallDialog, x => x != null);
            PrintStickerFragileCommand =
                new AsyncCommand<PrintStickerParameter>(PrintStickerFragileAsync, CanPrintSticker);
            PrintStickerWayUpCommand = new AsyncCommand<PrintStickerParameter>(PrintStickerWayUpAsync, CanPrintSticker);
            SpecifyBacklogTaskCommand = new AsyncCommand<BacklogTaskViewItem>(
                SpecifyAsync,
                CanSpecifyBacklogTask);
            TransferToItBacklogTaskCommand = new AsyncCommand<BacklogTaskViewItem>(
                TransferToItBacklogTaskAsync,
                CanTransferToItBacklogTask);
            CreateInJiraBacklogTaskCommand = new AsyncCommand<BacklogTaskViewItem>(
                CreateInJiraBacklogTaskAsync,
                CanCreateInJiraBacklogTask);
            AddToPlanBacklogTaskCommand = new AsyncCommand<BacklogTaskViewItem>(
                AddToPlanBacklogTaskAsync,
                CanAddToPlanBacklogTask);
            CancelBacklogTaskCommand = new AsyncCommand<BacklogTaskViewItem>(
                CancelBacklogTaskAsync,
                CanCancelBacklogTask);
            CompleteBacklogTaskCommand = new AsyncCommand<BacklogTaskViewItem>(
                CompleteBacklogTaskAsync,
                CanCompleteBacklogTask);
            RealizeBacklogTaskCommand = new AsyncCommand<BacklogTaskViewItem>(
                RealizeBacklogTaskAsync,
                CanRealizeBackLogTask);
        }

        public IAsyncCommand RealizeBacklogTaskCommand { get; }

        public IAsyncCommand CompleteBacklogTaskCommand { get; }

        public IAsyncCommand CancelBacklogTaskCommand { get; }

        public IAsyncCommand AddToPlanBacklogTaskCommand { get; }

        public IAsyncCommand CreateInJiraBacklogTaskCommand { get; }

        public IAsyncCommand SpecifyBacklogTaskCommand { get; }

        public IAsyncCommand TransferToItBacklogTaskCommand { get; }

        public IDelegateCommand ShowSerialNumberHistoryCommand { get; }

        public IDelegateCommand ShowProductCardCommand { get; }

        public IDelegateCommand ShowOrderCommand { get; }

        public IAsyncCommand ShowCreateReturnInvoiceCommand { get; }

        public IDelegateCommand ShowServiceRequestCommand { get; }

        public IDelegateCommand ShowTradeInCommand { get; }

        public IDelegateCommand ShowPhoneHistoryCommand { get; }

        public IDelegateCommand ShowCallDialogCommand { get; }

        public IAsyncCommand PrintStickerFragileCommand { get; }

        public IAsyncCommand PrintStickerWayUpCommand { get; }

        private IMessageFacadeService MessageFacadeService { get; }

        private IReadOnlyCollection<CarryType> CarryTypes { get; set; }

        private IMessenger Messenger { get; }

        private IWebClient WebClient { get; }

        private IErrorHandler ErrorHandler { get; }

        private IDictionaries Dictionaries { get; }

        private IPrintingSettingsStore PrintingSettingsStore { get; }

        private IMediator Mediator { get; }

        public async Task InitAsync(ISupportServices supportServices, IDocumentManagerService dialogDocumentManagerService)
        {
            _supportServices = supportServices;
            _dialogDocumentManagerService = dialogDocumentManagerService;

            await Task.WhenAll(RefreshEmployeesAsync(), RefreshEmployeeDepartmentsAsync());

            async Task RefreshEmployeesAsync()
            {
                List<EmployeeDto> employees = await WebClient.ExecuteApiRequestAsync(new QueryEmployees(), true).GetPagedResultDataAsync();

                _employees = employees.ToDictionary(x => x.Id);
            }

            async Task RefreshEmployeeDepartmentsAsync()
            {
                List<DepartmentDto> departments = await WebClient.ExecuteApiRequestAsync(new QueryDepartments());

                _headEmployeeDepartments = departments
                    .Where(x => x.EmployeeId.HasValue)
                    .GroupBy(x => x.EmployeeId.Value)
                    .ToDictionary(x => x.Key, x => x.Select(z => z.Id).ToHashSet());
            }
        }

        private static Image BitmapImage2Bitmap(BitmapImage bitmapImage)
        {
            using MemoryStream outStream = new();

            BmpBitmapEncoder enc = new();

            enc.Frames.Add(BitmapFrame.Create(bitmapImage));

            enc.Save(outStream);

            return Image.FromStream(outStream, true, true);
        }

        private static bool CanShowPhoneHistory(object obj)
        {
            return (obj is string[] phones && phones.Any(x => !string.IsNullOrWhiteSpace(x)))
                   || (obj is string phone && !string.IsNullOrWhiteSpace(phone));
        }

        private bool CanAddToPlanBacklogTask(BacklogTaskViewItem viewItem)
        {
            return viewItem != null
                   && viewItem.State.Id == BacklogTaskState.Documented.Id
                   && WebClient.IsOperationAllowed(BusinessOperation.BackLogTaskAddToPlan);
        }

        private bool CanCreateInJiraBacklogTask(BacklogTaskViewItem viewItem)
        {
            return viewItem != null
                   && viewItem.State.Id == BacklogTaskState.Formulated.Id
                   && WebClient.IsOperationAllowed(BusinessOperation.BackLogTaskCreateInJira);
        }

        private bool CanRealizeBackLogTask(BacklogTaskViewItem viewItem)
        {
            return viewItem != null
                   && viewItem.State.Id == BacklogTaskState.InProgress.Id
                   && WebClient.IsOperationAllowed(BusinessOperation.BackLogTaskRealize);
        }

        private bool CanCancelBacklogTask(BacklogTaskViewItem viewItem)
        {
            return viewItem != null
                   && (((viewItem.State.Id == BacklogTaskState.Idea.Id
                         || viewItem.State.Id == BacklogTaskState.Specify.Id
                         || viewItem.State.Id == BacklogTaskState.Formulated.Id)
                        && (viewItem.CreatedBy == WebClient.AuthenticatedEmployee.Id || IsHeadOfDepartmentBacklogTask(viewItem)))
                       || (WebClient.IsOperationAllowed(BusinessOperation.BackLogTaskCancel)
                           && viewItem.State.Id != BacklogTaskState.Canceled.Id
                           && viewItem.State.Id != BacklogTaskState.Completed.Id));
        }

        private bool CanSpecifyBacklogTask(BacklogTaskViewItem viewItem)
        {
            return viewItem != null
                   && (viewItem.State.Id == BacklogTaskState.Idea.Id
                       || viewItem.State.Id == BacklogTaskState.Formulated.Id)
                   && (viewItem.CreatedBy == WebClient.AuthenticatedEmployee.Id || IsHeadOfDepartmentBacklogTask(viewItem));
        }

        private bool CanCompleteBacklogTask(BacklogTaskViewItem viewItem)
        {
            return viewItem != null
                   && viewItem.State.Id == BacklogTaskState.Realized.Id
                   && (viewItem.CreatedBy == WebClient.AuthenticatedEmployee.Id || IsHeadOfDepartmentBacklogTask(
                       viewItem));
        }

        private bool CanTransferToItBacklogTask(BacklogTaskViewItem viewItem)
        {
            return viewItem != null
                   && (viewItem.State.Id == BacklogTaskState.Idea.Id
                       || viewItem.State.Id == BacklogTaskState.Specify.Id)
                   && IsHeadOfDepartmentBacklogTask(viewItem);
        }

        private bool IsHeadOfDepartmentBacklogTask(
            BacklogTaskViewItem viewItem)
        {
            return _employees is not null
                   && _headEmployeeDepartments is not null
                   && (WebClient.IsOperationAllowed(BusinessOperation.BackLogTaskDepartmentHead)
                       || (_headEmployeeDepartments.TryGetValue(WebClient.AuthenticatedEmployee.Id, out HashSet<int> departments)
                           && departments.Contains(_employees[viewItem.CreatedBy].DepartmentId)));
        }

        private void ShowSerialNumberHistory(string serialNumber)
        {
            if (string.IsNullOrWhiteSpace(serialNumber))
            {
                MessageFacadeService.ShowNotificationWarning("SN не заполнен");
            }
            else
            {
                Messenger.Send(new SerialNumberHistoryViewMessage(serialNumber));
            }
        }

        private void ShowProductCard(int? productId)
        {
            if (productId.HasValue)
            {
                Messenger.Send(new ProductCardViewMessage(productId.Value));
            }
        }

        private void ShowOrder(int? orderId)
        {
            if (orderId.HasValue)
            {
                Messenger.Send(new OrderEditViewMessage(orderId.Value));
            }
        }

        private async Task ShowCreateReturnInvoiceAsync(int? invoiceId)
        {
            try
            {
                Result<InvoiceDto> result = await WebClient.ExecuteApiRequestAsync(new SetInvoiceReturnedProducts(invoiceId!.Value));

                InvoiceDto invoiceDto = result.Data;

                if (invoiceDto.StateId != InvoiceState.Received.Id)
                {
                    MessageFacadeService.ShowMessageBoxError("Указана накладная не в статусе \"Принята\"");
                    return;
                }

                int productsCount = invoiceDto.InvoiceProducts.Count(x => x.QuantityReal - x.QuantityReturned > 0);

                if (productsCount == 0)
                {
                    MessageFacadeService.ShowNotificationWarning("Возвращать нечего");
                    return;
                }

                Messenger.Send(new CreateReturnInvoiceMessage(invoiceDto));
            }
            catch (UnexpectedSatusException exception) when (exception.Message == "An unexpected status code was returned. (NotFound Resource not found)")
            {
                MessageFacadeService.ShowMessageBoxError("Указанной накладной не существует");
            }
        }

        private void ShowServiceRequest(int? serviceRequestId)
        {
            if (serviceRequestId.HasValue)
            {
                Messenger.Send(new ServiceRequestViewMessage(serviceRequestId.Value));
            }
        }

        private void ShowTradeIn(int? tradeInId)
        {
            if (tradeInId.HasValue)
            {
                Messenger.Send(new TradeInViewMessage(tradeInId.Value));
            }
        }

        private void ShowPhoneHistory(object obj)
        {
            PhoneHistoryViewMessage message;

            switch (obj)
            {
                case string[] phones when phones.Any(x => !string.IsNullOrWhiteSpace(x)):
                    message = new PhoneHistoryViewMessage(phones);
                    break;
                case string phone when !string.IsNullOrWhiteSpace(phone):
                    message = new PhoneHistoryViewMessage(phone);
                    break;
                default:
                    message = null;
                    break;
            }

            if (message != null)
            {
                Messenger.Send(message);
            }
        }

        private void ShowCallDialog(CallDialogParameter parameter)
        {
            Messenger.Send(parameter);
        }

        private bool CanPrintSticker(PrintStickerParameter obj)
        {
            if (obj == null || obj.CarryId is null)
            {
                return false;
            }

            CarryTypes ??= Dictionaries.GetItems<CarryType>();

            CarryType carryType = CarryTypes.FirstOrDefault(x => x.Id == obj.CarryId.Value);

            return carryType?.StickerRequired == true;
        }

        private Task PrintStickerFragileAsync(PrintStickerParameter arg)
        {
            return PrintStickerAsync("fragile.png", arg.ShowPreview);
        }

        private Task PrintStickerWayUpAsync(PrintStickerParameter arg)
        {
            return PrintStickerAsync("thiswayup.png", arg.ShowPreview);
        }

        private async Task PrintStickerAsync(string fileName, bool showPreview)
        {
            Uri uri = new Uri($@"pack://application:,,,/Images/CarryStiker/{fileName}");

            BitmapImage imageSource = new BitmapImage(uri);

            Image image = BitmapImage2Bitmap(imageSource);

            PrintingSettingsInfo printingSettings = await PrintingSettingsStore.LoadAsync();

            StickerReportData data = new StickerReportData(980, 1000, image);

            StickerReport stickerReport = new StickerReport()
            {
                DataSource = new[] { data }
            };

            PrintReportRequest request = new(
                stickerReport,
                showPreview,
                printingSettings.Sticker.Name,
                printingSettings.Sticker.PaperSource);

            await System.Windows.Application.Current.Dispatcher.InvokeAsync(
                async () => await Mediator.Send(request, CancellationToken.None));
        }

        private async Task SpecifyAsync(BacklogTaskViewItem viewItem)
        {
            Result<BacklogTaskDto> result = await ErrorHandler.HandleErrorsAsync(
                _ => WebClient.ExecuteApiRequestAsync(new SpecifyBacklogTask(viewItem.Id)),
                "уточнении",
                "Задача уточнена",
                _supportServices,
                true,
                showNotification: true);

            SendBacklogMessage(result);
        }

        private async Task TransferToItBacklogTaskAsync(BacklogTaskViewItem viewItem)
        {
            Result<BacklogTaskDto> result = await ErrorHandler.HandleErrorsAsync(
                _ => WebClient.ExecuteApiRequestAsync(new TransferToItBacklogTask(viewItem.Id)),
                "передаче задачи в IT",
                "Задача передана в IT",
                _supportServices,
                true,
                showNotification: true);

            SendBacklogMessage(result);
        }

        private async Task CreateInJiraBacklogTaskAsync(BacklogTaskViewItem viewItem)
        {
            BacklogTaskJiraViewModel viewModel = _dialogDocumentManagerService.ShowView<BacklogTaskJiraViewModel>(new BacklogTaskJiraParameter(viewItem), _supportServices);

            if (!viewModel.IsOk)
            {
                return;
            }

            Result<BacklogTaskDto> result = await ErrorHandler.HandleErrorsAsync(
                _ => WebClient.ExecuteApiRequestAsync(new CreateInJiraBacklogTask(viewItem.Id, new CreateInJiraBacklogDto()
                {
                    JiraId = viewModel.JiraId,
                    Estimate = viewModel.Estimate
                })),
                "создании в Jira",
                "Задача создана в Jira",
                _supportServices,
                true,
                showNotification: true);

            SendBacklogMessage(result);
        }

        private async Task AddToPlanBacklogTaskAsync(BacklogTaskViewItem viewItem)
        {
            BacklogTaskAddToPlanViewModel viewModel = _dialogDocumentManagerService.ShowView<BacklogTaskAddToPlanViewModel>(
                new BacklogTaskAddToPlanParameter(viewItem),
                _supportServices);

            if (!viewModel.IsOk)
            {
                return;
            }

            Result<BacklogTaskDto> result = await ErrorHandler.HandleErrorsAsync(
                _ => WebClient.ExecuteApiRequestAsync(
                    new AddToPlanBacklogTask(
                        viewItem.Id,
                        new AddToPlanBacklogTaskDto()
                        {
                            Estimate = viewModel.Estimate!.Value,
                            QuotaId = viewModel.SelectedQuotaId!.Value
                        })),
                "добавлении в план",
                "Задача добавлена в план",
                _supportServices,
                true,
                showNotification: true);

            SendBacklogMessage(result);
        }

        private async Task CancelBacklogTaskAsync(BacklogTaskViewItem viewItem)
        {
            if (!MessageFacadeService.Confirm("Вы уверены, что хотите отменить задачу?"))
            {
                return;
            }

            Result<BacklogTaskDto> result = await ErrorHandler.HandleErrorsAsync(
                _ => WebClient.ExecuteApiRequestAsync(new CancelBacklogTask(viewItem.Id)),
                "отмене",
                "Задача отменена",
                _supportServices,
                true,
                showNotification: true);

            SendBacklogMessage(result);
        }

        private async Task CompleteBacklogTaskAsync(BacklogTaskViewItem viewItem)
        {
             Result<BacklogTaskDto> result = await ErrorHandler.HandleErrorsAsync(
                _ => WebClient.ExecuteApiRequestAsync(new CompleteBacklogTask(viewItem.Id)),
                "завершении",
                "Задача завершена",
                _supportServices,
                true,
                showNotification: true);

             SendBacklogMessage(result);
        }

        private async Task RealizeBacklogTaskAsync(BacklogTaskViewItem viewItem)
        {
            Result<BacklogTaskDto> result = await ErrorHandler.HandleErrorsAsync(
                _ => WebClient.ExecuteApiRequestAsync(new RealizeBacklogTask(viewItem.Id)),
                "реализации",
                "Задача реализована",
                _supportServices,
                true,
                showNotification: true);

            SendBacklogMessage(result);
        }

        private void SendBacklogMessage(Result<BacklogTaskDto> result)
        {
            if (result?.IsSuccess == true)
            {
                Messenger.Send(new BacklogTaskMessage(result.Data, MessageType.Changed));
            }
        }
    }
}