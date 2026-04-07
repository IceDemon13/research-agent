using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using DevExpress.Xpf.Core;
using Microsoft.Extensions.Logging;
using Telemart.Client.Common.ErrorHandler;
using Telemart.Client.Common.Messages;
using Telemart.Client.Common.Services;
using Telemart.Client.Data.Requests.Features.Segment;
using Telemart.Client.Data.Requests.Features.Segment.Actions;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Data.WebClient.Security;
using Telemart.Client.Extensions;
using Telemart.Client.Properties;
using Telemart.Client.TransferObjects.Segment;

namespace Telemart.Client.ViewModels.Segment
{
    public sealed class SegmentsViewModel : ViewModelBase, ISupportHotkeys
    {
        public SegmentsViewModel(
            IWebClient webClient,
            IMessenger messenger,
            IMessageFacadeService messageFacadeService,
            IMapper mapper,
            IErrorHandler errorHandler,
            ILogger<SegmentsViewModel> logger)
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
            EditCommand = new DelegateCommand(Edit, () => SelectedSegment != null);
            CreateCommand = new DelegateCommand(Create);
            DeleteCommand = new AsyncCommand(DeleteAsync, () => WebClient.IsOperationAllowed(BusinessOperation.DeleteSegment) && SelectedSegment != null);
            CategorySettingsCommand = new DelegateCommand(ShowCategorySettings, () => WebClient.IsOperationAllowed(BusinessOperation.SegmentModuleAccess));
            RecalculateCommand = new DelegateCommand(Recalculate, () => WebClient.IsOperationAllowed(BusinessOperation.RecalculateSegments));
            RecalculateAbcCommand = new AsyncCommand(RecalculateAbcAsync, () => WebClient.IsOperationAllowed(BusinessOperation.RecalculateSegmentsAbc));

            Messenger.Register<EntityMessage<SegmentDto>>(this, OnSegmentMessage);
        }

        public SegmentsViewModel()
        {
        }

        #region Commands

        public IAsyncCommand RefreshCommand { get; }

        public IAsyncCommand FetchNewFeatures { get; }

        public IDelegateCommand CreateCommand { get; }

        public IDelegateCommand HandleLoadedCommand { get; }

        public IDelegateCommand CategorySettingsCommand { get; }

        public IDelegateCommand RecalculateCommand { get; }

        public IAsyncCommand RecalculateAbcCommand { get; }

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

        public SegmentViewItem SelectedSegment
        {
            get { return GetProperty(() => SelectedSegment); }
            set { SetProperty(() => SelectedSegment, value); }
        }

        public ObservableCollection<SegmentViewItem> Segments
        {
            get { return GetProperty(() => Segments); }
            set { SetProperty(() => Segments, value); }
        }

        private IWebClient WebClient { get; }

        private IMessenger Messenger { get; }

        private IErrorHandler ErrorHandler { get; }

        private IMapper Mapper { get; }

        private IMessageFacadeService MessageFacadeService { get; }

        private IDocumentManagerService SizeableDialogDocumentManagerService => GetService<IDocumentManagerService>("SizeableDialogDocumentManagerService", ServiceSearchMode.PreferParents);

        private ILogger<SegmentsViewModel> Logger { get; }

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

        private void HandleLoaded()
        {
            if (Segments == null)
            {
                Segments = new ObservableCollection<SegmentViewItem>();
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

                List<SegmentDto> segments = await WebClient.ExecuteApiRequestAsync(new QuerySegments());

                Segments = segments.Select(x => Mapper.Map<SegmentViewItem>(x)).ToObservableCollection();

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

        private async Task FetchNewFeaturesAsync()
        {
            List<SegmentCategorySettingsDto> segmentCategorySettings = await WebClient.ExecuteApiRequestAsync(new QuerySegmentCategorySettings());

            foreach (SegmentViewItem segment in Segments)
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
            if (!WebClient.IsOperationAllowed(BusinessOperation.UpdateSegment))
            {
                MessageFacadeService.ShowNotificationError("У вас нет прав на выполнение операции");
                return;
            }

            if (!WebClient.IsOperationAllowed(BusinessOperation.AllowEditAllSegments) && SelectedSegment.EmployeeId.Value != WebClient.AuthenticatedEmployee.Id)
            {
                MessageFacadeService.ShowNotificationWarning("Вы не являетесь ответственным за этот сегмент");
                return;
            }

            SizeableDialogDocumentManagerService.ShowView<SegmentViewModel>(new SegmentParameter(SelectedSegment.Id), this);
        }

        private void Create()
        {
            if (!WebClient.IsOperationAllowed(BusinessOperation.CreateSegment))
            {
                MessageFacadeService.ShowNotificationError("У вас нет прав на выполнение операции");
                return;
            }

            SizeableDialogDocumentManagerService.ShowView<SegmentViewModel>(new SegmentParameter(0), this);
        }

        private async Task DeleteAsync()
        {
            if (!MessageFacadeService.Confirm("Вы уверены?"))
            {
                return;
            }

            if (!WebClient.IsOperationAllowed(BusinessOperation.AllowEditAllSegments) && SelectedSegment.EmployeeId.Value != WebClient.AuthenticatedEmployee.Id)
            {
                MessageFacadeService.ShowNotificationWarning("Вы не являетесь ответственным за этот сегмент");
                return;
            }

            object result = await ErrorHandler.HandleErrorsAsync(_ => WebClient.ExecuteApiRequestAsync(new DeleteSegment(SelectedSegment.Id)), "удалении сегмента", "Сегмент удален", this, true);

            if (result != null)
            {
                Segments.Remove(SelectedSegment);
            }
        }

        private void ShowCategorySettings()
        {
            if (!WebClient.IsOperationAllowed(BusinessOperation.UpdateSegmentCategorySettings))
            {
                MessageFacadeService.ShowNotificationError("У вас нет прав на выполнение операции");
                return;
            }

            SizeableDialogDocumentManagerService.ShowView<SegmentCategorySettingsViewModel>(null, this);

            FetchNewFeatures.Execute(null);
        }

        private void Recalculate()
        {
            SizeableDialogDocumentManagerService.ShowView<RecalculateSegmentsViewModel>(null, this);
        }

        private async Task RecalculateAbcAsync()
        {
            SplashScreenManager splashScreenManager = SplashScreenManager.CreateWaitIndicator();
            splashScreenManager.Show();

            await ErrorHandler.HandleErrorsAsync(_ => WebClient.ExecuteApiRequestAsync(new RecalculateAbcSegments()), "пересчете ABC классов товаров в сегменте", "Товары в сегменте пересчитаны", this, true);

            await ErrorHandler.HandleErrorsAsync(_ => WebClient.ExecuteApiRequestAsync(new RecalculateAbcCategorySegments()), "пересчете ABC классов сегментов в категории", "Сегменты в категории пересчитаны", this, true);

            await ErrorHandler.HandleErrorsAsync(_ => WebClient.ExecuteApiRequestAsync(new RecalculateAbcProductCategorySegments()), "пересчете ABC классов товаров в категории", "Товары в категории пересчитаны", this, true);

            splashScreenManager.Close();
        }

        private void OnSegmentMessage(EntityMessage<SegmentDto> message)
        {
            Segments ??= new ObservableCollection<SegmentViewItem>();

            SegmentDto dto = message.Entity;

            switch (message.MessageType)
            {
                case MessageType.Added:
                    Segments.Add(Mapper.Map<SegmentViewItem>(dto));
                    break;
                case MessageType.Changed:
                    Segments.DoActionWithItem(x => x.Id == dto.Id, x => Mapper.Map(dto, x));
                    break;
            }
        }
    }
}