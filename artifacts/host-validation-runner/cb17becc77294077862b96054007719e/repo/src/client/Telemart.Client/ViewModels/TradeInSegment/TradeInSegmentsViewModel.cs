using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using Microsoft.Extensions.Logging;
using Telemart.Client.Common.ErrorHandler;
using Telemart.Client.Common.Messages;
using Telemart.Client.Common.Services;
using Telemart.Client.Data.Requests.Features.ConstantQueries;
using Telemart.Client.Data.Requests.Features.Setting;
using Telemart.Client.Data.Requests.Features.TradeInSegment;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Data.WebClient.Security;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.Properties;
using Telemart.Client.TransferObjects.TradeInSegment;
using Telemart.Client.ViewModels.TradeIn;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.ViewModels.TradeInSegment
{
    public sealed class TradeInSegmentsViewModel : ViewModelBase, ISupportHotkeys
    {
        public TradeInSegmentsViewModel(
            IWebClient webClient,
            IMessenger messenger,
            IMessageFacadeService messageFacadeService,
            IMapper mapper,
            IErrorHandler errorHandler,
            ILogger<TradeInSegmentsViewModel> logger)
        {
            WebClient = webClient ?? throw new ArgumentNullException(nameof(webClient));
            Mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            Messenger = messenger ?? throw new ArgumentNullException(nameof(messenger));
            MessageFacadeService = messageFacadeService ?? throw new ArgumentNullException(nameof(messageFacadeService));
            ErrorHandler = errorHandler ?? throw new ArgumentNullException(nameof(errorHandler));
            Logger = logger;

            FetchNewFeatures = new AsyncCommand(FetchNewFeaturesAsync);
            RefreshCommand = new AsyncCommand(RefreshAsync);
            HandleLoadedCommand = new DelegateCommand(HandleLoaded);
            OpenCoefsEditorCommand = new DelegateCommand(OpenCoefsEditor, () => WebClient.IsOperationAllowed(BusinessOperation.TradeInSetCoefs));
            EditCommand = new DelegateCommand(Edit, () => WebClient.IsOperationAllowed(BusinessOperation.UpdateTradeInSegment) && SelectedSegment != null);
            CreateCommand = new DelegateCommand(Create, () => WebClient.IsOperationAllowed(BusinessOperation.CreateTradeInSegment));
            DeleteCommand = new AsyncCommand(DeleteAsync, () => WebClient.IsOperationAllowed(BusinessOperation.DeleteTradeInSegment) && SelectedSegment != null);
            CategorySettingsCommand = new DelegateCommand(ShowCategorySettings, () => WebClient.IsOperationAllowed(BusinessOperation.UpdateTradeInSegmentCategorySettings));
            RecalculateCommand = new DelegateCommand(Recalculate, () => WebClient.IsOperationAllowed(BusinessOperation.RecalculateTradeInSegments));
            ChangeAutoEvaluationCommand = new AsyncCommand(ChangeAutoEvaluationAsync, () => WebClient.AuthenticatedEmployee.Roles.Any(x => x == Role.Admin.Name || x == Role.TradeIn.Name));

            Messenger.Register<EntityMessage<TradeInSegmentDto>>(this, OnSegmentMessage);
        }

        public TradeInSegmentsViewModel()
        {
        }

        #region Commands

        public IAsyncCommand RefreshCommand { get; }

        public IAsyncCommand FetchNewFeatures { get; }

        public IDelegateCommand CreateCommand { get; }

        public IDelegateCommand HandleLoadedCommand { get; }

        public IDelegateCommand OpenCoefsEditorCommand { get; }

        public IDelegateCommand CategorySettingsCommand { get; }

        public IDelegateCommand RecalculateCommand { get; }

        public IAsyncCommand ChangeAutoEvaluationCommand { get; }

        public IDelegateCommand EditCommand { get; }

        public IAsyncCommand DeleteCommand { get; }

        #endregion

        public bool IsLongOperationInProgress
        {
            get { return GetProperty(() => IsLongOperationInProgress); }
            set { SetProperty(() => IsLongOperationInProgress, value); }
        }

        public bool IsColumnChooserVisible
        {
            get { return GetProperty(() => IsColumnChooserVisible); }
            set { SetProperty(() => IsColumnChooserVisible, value); }
        }

        public TradeInSegmentViewItem SelectedSegment
        {
            get { return GetProperty(() => SelectedSegment); }
            set { SetProperty(() => SelectedSegment, value); }
        }

        public ObservableCollection<TradeInSegmentViewItem> Segments
        {
            get { return GetProperty(() => Segments); }
            set { SetProperty(() => Segments, value); }
        }

        public bool AutoEvaluation
        {
            get { return GetProperty(() => AutoEvaluation); }
            set { SetProperty(() => AutoEvaluation, value, () => RaisePropertyChanged(nameof(ChangeAutoEvaluationButtonContent))); }
        }

        public string ChangeAutoEvaluationButtonContent => AutoEvaluation ? "Выключить" : "Включить";

        private IWebClient WebClient { get; }

        private IMessenger Messenger { get; }

        private IErrorHandler ErrorHandler { get; }

        private IMapper Mapper { get; }

        private IMessageFacadeService MessageFacadeService { get; }

        private IDocumentManagerService SizeableDialogDocumentManagerService => GetService<IDocumentManagerService>("SizeableDialogDocumentManagerService", ServiceSearchMode.PreferParents);

        private IDocumentManagerService DialogDocumentManagerService => GetService<IDocumentManagerService>("DialogDocumentManagerService", ServiceSearchMode.PreferParents);

        private ILogger<TradeInSegmentsViewModel> Logger { get; }

        public bool HandleHotkey(HotkeyMessage hotkeyMessage)
        {
            bool handled = false;

            switch (hotkeyMessage.HotkeyMessageType)
            {
                case HotkeyMessageType.Refresh:
                    RefreshCommand.Execute(null);
                    handled = true;
                    break;

                case HotkeyMessageType.Edit:
                    EditCommand.Execute(null);
                    handled = true;
                    break;

                case HotkeyMessageType.ShowColumnChooser:
                    IsColumnChooserVisible = !IsColumnChooserVisible;
                    handled = true;
                    break;

                case HotkeyMessageType.Add:
                    CreateCommand.Execute(null);
                    handled = true;
                    break;
            }

            return handled;
        }

        private void OpenCoefsEditor()
        {
            DialogDocumentManagerService.ShowView<TradeInCoefViewModel>(null, this);
        }

        private void HandleLoaded()
        {
            if (Segments == null)
            {
                Segments = new ObservableCollection<TradeInSegmentViewItem>();
            }

            if (Segments.Any())
            {
                return;
            }

            RefreshCommand.Execute(null);
        }

        private async Task RefreshAsync()
        {
            IsLongOperationInProgress = true;

            try
            {
                Segments = null;

                List<TradeInSegmentDto> segments = await WebClient.ExecuteApiRequestAsync(new QueryTradeInSegments());

                Segments = segments.Select(x => Mapper.Map<TradeInSegmentViewItem>(x)).ToObservableCollection();

                await LoadAutoEvaluationFlagAsync();

                FetchNewFeatures.Execute(null);
            }
            catch (Exception exception)
            {
                MessageFacadeService.ShowNotificationError(Resources.ErrorDuringDataLoading);
                Logger.LogError(exception, "Exception while refreshing grid");
            }
            finally
            {
                IsLongOperationInProgress = false;
            }
        }

        private async Task LoadAutoEvaluationFlagAsync()
        {
            object tradeInAutoEvaluationObj = await ErrorHandler.HandleErrorsAsync(
                _ => WebClient.ExecuteApiRequestAsync(new QueryConstant(ConstantKeys.TradeInAutoEvaluation)),
                "получении флага автооценки",
                null,
                this,
                true,
                showDialog: false,
                showNotification: false);

            if (tradeInAutoEvaluationObj is null)
            {
                return;
            }

            if (int.TryParse(tradeInAutoEvaluationObj.ToString(), out int autoEvaluation))
            {
                AutoEvaluation = autoEvaluation > 0;
            }
            else
            {
                MessageFacadeService.ShowNotificationError("Ошибка при получении параметра автооценки");
            }
        }

        private async Task FetchNewFeaturesAsync()
        {
            List<TradeInSegmentCategorySettingsDto> segmentCategorySettings = await WebClient.ExecuteApiRequestAsync(new QueryTradeInSegmentCategorySettings());

            foreach (TradeInSegmentViewItem segment in Segments)
            {
                if (!segmentCategorySettings
                    .Where(z => z.CategoryId == segment.CategoryId)
                    .All(s => segment.CategoryFeatures
                        .Select(f => f.FeatureId)
                        .Contains(s.FeatureId)))
                {
                    segment.AnyNewFeatures = true;
                }
                else
                {
                    segment.AnyNewFeatures = false;
                }
            }
        }

        private void Edit()
        {
            if (!WebClient.IsOperationAllowed(BusinessOperation.UpdateTradeInSegment))
            {
                MessageFacadeService.ShowNotificationError("У вас нет прав на выполнение операции");
                return;
            }

            if (!WebClient.IsOperationAllowed(BusinessOperation.AllowEditAllTradeInSegments) && SelectedSegment.EmployeeId.Value != WebClient.AuthenticatedEmployee.Id)
            {
                MessageFacadeService.ShowNotificationWarning("Вы не являетесь ответственным за этот сегмент");
                return;
            }

            SizeableDialogDocumentManagerService.ShowView<TradeInSegmentViewModel>(new TradeInSegmentParameter(SelectedSegment.Id), this);
        }

        private void Create()
        {
            if (!WebClient.IsOperationAllowed(BusinessOperation.CreateTradeInSegment))
            {
                MessageFacadeService.ShowNotificationError("У вас нет прав на выполнение операции");
                return;
            }

            SizeableDialogDocumentManagerService.ShowView<TradeInSegmentViewModel>(new TradeInSegmentParameter(0), this);
        }

        private async Task DeleteAsync()
        {
            if (!MessageFacadeService.Confirm("Вы уверены?"))
            {
                return;
            }

            object result = await ErrorHandler.HandleErrorsAsync(_ => WebClient.ExecuteApiRequestAsync(new DeleteTradeInSegment(SelectedSegment.Id)), "удалении сегмента", "Сегмент удален", this, true);

            if (result != null)
            {
                Segments.Remove(SelectedSegment);
            }
        }

        private void ShowCategorySettings()
        {
            if (!WebClient.IsOperationAllowed(BusinessOperation.UpdateTradeInSegmentCategorySettings))
            {
                MessageFacadeService.ShowNotificationError("У вас нет прав на выполнение операции");
                return;
            }

            SizeableDialogDocumentManagerService.ShowView<TradeInSegmentCategorySettingsViewModel>(null, this);

            FetchNewFeatures.Execute(null);
        }
        
        private async Task ChangeAutoEvaluationAsync()
        {
            Result result = await ErrorHandler.HandleErrorsAsync(
                _ => WebClient.ExecuteApiRequestAsync(new ChangeTradeInAutoEvaluation()),
                "при сохранении режима автооценки",
                "Параметр автооценки сохранен",
                this,
                true,
                showNotification: true);

            if (result?.IsSuccess == true)
            {
                await LoadAutoEvaluationFlagAsync();
            }
        }

        private void Recalculate()
        {
            SizeableDialogDocumentManagerService.ShowView<RecalculateTradeInSegmentsViewModel>(null, this);
        }

        private void OnSegmentMessage(EntityMessage<TradeInSegmentDto> message)
        {
            Segments ??= new ObservableCollection<TradeInSegmentViewItem>();

            TradeInSegmentDto dto = message.Entity;

            switch (message.MessageType)
            {
                case MessageType.Added:
                    Segments.Add(Mapper.Map<TradeInSegmentViewItem>(dto));
                    break;
                case MessageType.Changed:
                    Segments.DoActionWithItem(x => x.Id == dto.Id, x => Mapper.Map(dto, x));
                    break;
            }
        }
    }
}