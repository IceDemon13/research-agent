using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using AutoMapper;
using DevExpress.Mvvm;
using DevExpress.Mvvm.UI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SmartFormat;
using Telemart.Client.Common;
using Telemart.Client.Common.Messages;
using Telemart.Client.Common.Services;
using Telemart.Client.Common.Settings.ModuleAnalytics;
using Telemart.Client.Core.Exceptions;
using Telemart.Client.Data.Requests.Features.ModuleAnalyticUrl;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.Helpers;
using Telemart.Client.Properties;
using Telemart.Client.TransferObjects;
using Telemart.Client.ViewModels.ModuleAnalytics;
using Telemart.Client.ViewModels.Validation;
using Telemart.Common.ErrorHandling;
using Application = System.Windows.Application;
using ModuleAnalyticsPosition = Telemart.Client.Dictionaries.ModuleAnalyticsPosition;

namespace Telemart.Client.ViewModels.Base
{
    public abstract class TelemartEditorViewModelBase<TDto, TParam, TViewItem> : TelemartDialogViewModelBase
        where TDto : class, new()
        where TParam : class, IEditorParameter
        where TViewItem : ILockableEntity, INotifyPropertyChanged, IDataErrorInfo, ICloneable, new()
    {
        private TelemartCompareHelper<TViewItem> compareHelper;

        protected TelemartEditorViewModelBase(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            IMapper mapper,
            IMessenger messenger)
            : base(webClient, dictionaries, messageFacadeService)
        {
            Mapper = mapper;
            Messenger = messenger;

            LockCommand = new AsyncCommand(LockAsync, CanLock);
            UnlockCommand = new AsyncCommand(UnlockAsync, CanUnlock);
            SaveCommand = new AsyncCommand(SaveAsync);
        }

        protected TelemartEditorViewModelBase()
        {
        }

        #region Commands

        public IAsyncCommand LockCommand { get; }

        public IAsyncCommand UnlockCommand { get; }

        public IAsyncCommand SaveCommand { get; }

        #endregion

        #region INPC

        public bool IsChanged => ManualModelChanged || CompareHelper.IsChanged();

        public bool IsLockedByCurrentEmployee => !LockSupport || (WebClient.AuthenticatedEmployee != null && WebClient.AuthenticatedEmployee.Id == Model?.EmployeeLockId);

        public bool IsLockedByEmployee => !LockSupport || Model?.EmployeeLockId != null;

        public bool IsNew => Model?.Id == 0;

        public bool IsCopy { get; private set; }

        public bool IsNewOrIsLockedByCurrentEmployee => IsNew || IsLockedByCurrentEmployee;

        public bool ManualModelChanged { get; set; } = false;

        public TViewItem Model
        {
            get { return GetProperty(() => Model); }
            private set { SetProperty(() => Model, value, () => RaisePropertiesChanged(nameof(IsLockedByCurrentEmployee), nameof(IsNew), nameof(IsNewOrIsLockedByCurrentEmployee))); }
        }

        public TParam EditorParameter
        {
            get { return GetProperty(() => EditorParameter); }
            private set { SetProperty(() => EditorParameter, value); }
        }

        public TViewItem ModelOriginal
        {
            get { return GetProperty(() => ModelOriginal); }
            private set { SetProperty(() => ModelOriginal, value); }
        }

        #endregion

        #region Services

        protected IMapper Mapper { get; }

        protected IMessenger Messenger { get; }

        #endregion

        protected abstract string CreatedActionMessage { get; }

        protected abstract string EntityName { get; }

        protected abstract string UpdatedActionMessage { get; }

        protected bool LockSupport { get; set; } = true;

        protected bool AutoUnlock { get; set; } = true;

        protected virtual bool UseStandartPropertyValidation { get; set; } = true;

        protected TelemartCompareHelper<TViewItem> CompareHelper => compareHelper ??= new TelemartCompareHelper<TViewItem>(ModelOriginal, Model, GetMembersToIgnore());

        public override void OnClose(CancelEventArgs e)
        {
            if (IsChanged && !MessageFacadeService.Confirm("Закрыть диалог без сохранения изменений?"))
            {
                e.Cancel = true;
            }
            else
            {
                Messenger.Send(new ModuleAnalyticsCloseMessage(((TParam)Parameter).Id));

                base.OnClose(e);
            }
        }

        public override void OnDestroy()
        {
            if (LockSupport && !IsNew && IsLockedByCurrentEmployee)
            {
                UnlockAsync().ContinueWith(x => base.OnDestroy(), TaskScheduler.FromCurrentSynchronizationContext());
            }

            base.OnDestroy();
        }

        protected override void OnParameterChanged(object parameter)
        {
            base.OnParameterChanged(parameter);

            EditorParameter = (TParam)Parameter;
        }

        protected override void OnInitializeInDesignMode()
        {
            base.OnInitializeInDesignMode();

            Model = new TViewItem();

            OnInitializeInDesignModeInternal();
        }

        protected virtual void OnInitializeInDesignModeInternal()
        {
        }

        protected virtual void AfterSetData()
        {
        }

        protected virtual void BeforeSetData(TViewItem model, object dto)
        {
        }

        protected abstract Task<Result<TDto>> CreateEntityAsync();

        protected virtual object CreateEntityMessage(TDto dto, MessageType messageType)
        {
            return new EntityMessage<TDto>(dto, messageType);
        }

        protected abstract Task<TDto> GetEntityAsync(int id);

        protected virtual IEnumerable<string> GetMembersToIgnore()
        {
            yield break;
        }

        protected override async Task HandleLoadedAsync()
        {
            TParam param = (TParam)Parameter;

            IsCopy = param is IEditorParameterCopy { Copy: true };

            if (param.IsNew)
            {
                Application.Current.Dispatcher.Invoke(() => SetData(param));
                SetCreateTitle();
            }
            else
            {
                TDto source = await GetEntityAsync(param.Id);

                SetData(source);

                if (!IsCopy)
                {
                    SetEditTitle();
                }

                try
                {
                    // Current logic also placed in OrderViewModel
                    List<ModuleAnalyticUrlDto> moduleAnalyticsUrls = await WebClient.ExecuteApiRequestAsync(new QueryModuleAnalyticUrls(), true);

                    ModuleAnalyticUrlDto analyticUrlDto = moduleAnalyticsUrls.FirstOrDefault(x => string.Equals(x.ModuleView, GetType().Name, StringComparison.OrdinalIgnoreCase));

                    if (analyticUrlDto is not null
                        && (WebClient.AuthenticatedEmployeeFullData?.Accounts?.Any(x => x.AccountId == WorkAccountIds.MetabaseId) == true
                            || WebClient.AuthenticatedEmployeeFullData?.GenericAccounts?.Metabase?.Login != null))
                    {
                        IModuleAnalyticsSettingsStore moduleAnalyticsSettingsStore = ServiceProvider.GetRequiredService<IModuleAnalyticsSettingsStore>();

                        ModuleAnalyticsSettings moduleAnalyticsSettings = await moduleAnalyticsSettingsStore.LoadAsync();

                        ModuleAnalyticsSetting setting = new()
                        {
                            PositionId = ModuleAnalyticsPosition.Maximized.Id,
                            LocationId = ModuleAnalyticsLocation.Horizontal.Id
                        };

                        setting = moduleAnalyticsSettings.Settings.GetValueOrDefault(GetType().Name, setting);

                        if (setting.PositionId == ModuleAnalyticsPosition.Hidden.Id)
                        {
                            return;
                        }

                        string analyticUrl = Smart.Format(analyticUrlDto.Url, Model);

                        // To support this feature - add <dxmvvm:CurrentWindowService /> behavior in XAML
                        CurrentWindowService currentWindowService = (CurrentWindowService)GetService<ICurrentWindowService>(ServiceSearchMode.PreferParents);

                        double? newLeft = null;
                        double? newTop = null;
                        int analyticsWindowWidth = Width;
                        int analyticsWindowHeight = Height;

                        if (setting.LocationId == ModuleAnalyticsLocation.Horizontal.Id)
                        {
                            if (currentWindowService?.ActualWindow is not null)
                            {
                                double currentTop = currentWindowService.ActualWindow.Top;
                                double currentLeft = currentWindowService.ActualWindow.Left;
                                double currentWidth = currentWindowService.ActualWindow.Width;

                                double screenHeight = SystemParameters.PrimaryScreenHeight;
                                double screenWidth = SystemParameters.PrimaryScreenWidth;

                                const int desiredHeight = 350;

                                newLeft = currentLeft;
                                newTop = currentTop + currentWindowService.ActualWindow.Height;

                                if (newTop + desiredHeight > screenHeight)
                                {
                                    newLeft = (screenWidth - currentWidth) / 2;
                                    newTop = (screenHeight - desiredHeight) / 2;
                                }

                                analyticsWindowHeight = desiredHeight;
                                analyticsWindowWidth = (int)currentWindowService.ActualWindow.Width - Constants.AnalyticsFormWidthMargin;
                            }
                        }
                        else
                        {
                            if (setting.Left.HasValue && setting.Top.HasValue)
                            {
                                newLeft = setting.Left.Value;
                                newTop = setting.Top.Value;
                                analyticsWindowWidth = setting.Width ?? analyticsWindowWidth;
                                analyticsWindowHeight = setting.Height ?? analyticsWindowHeight;
                            }
                            else
                            {
                                newLeft = 100;
                                newTop = 100;
                            }
                        }

                        const int defaultWindowMargin = 25;

                        ModuleAnalyticsParameter browserParameter = new ModuleAnalyticsParameter(
                            analyticsWindowWidth,
                            analyticsWindowHeight,
                            (int)(newTop ?? defaultWindowMargin),
                            (int)(newLeft ?? defaultWindowMargin),
                            analyticUrl,
                            "Аналитика",
                            setting.PositionId,
                            this,
                            param.Id,
                            GetType().Name);

                        Messenger.Send(browserParameter);
                    }
                }
                catch (Exception e)
                {
                    Logger.LogError(e, "Failed to load module analytics URL");
                    MessageFacadeService.ShowNotificationError("Не удалось загрузить аналитику");
                }
            }
        }

        protected override async Task HandleOkAsync()
        {
            bool processed = await SaveAsync();

            if (processed)
            {
                IsOk = true;
                Close();
            }
        }

        protected abstract Task<LockResponse<TDto>> LockEntityAsync(int id);

        protected abstract Task<LockResponse<TDto>> UnlockEntityAsync(int id);

        protected virtual void OnModelPropertyChangedInternal(object sender, PropertyChangedEventArgs e)
        {
        }

        protected virtual bool IsValid(TViewItem model)
        {
            return true;
        }

        protected virtual async Task<bool> SaveAsync()
        {
            if ((IDataErrorInfoHelper.HasErrors(Model) && UseStandartPropertyValidation) || !IsValid(Model))
            {
                return false;
            }

            bool processed = false;

            if (IsChanged)
            {
                try
                {
                    Task<Result<TDto>> saveTask;
                    MessageType messageType;
                    string messageAction;

                    if (IsNew)
                    {
                        saveTask = CreateEntityAsync();
                        messageType = MessageType.Added;
                        messageAction = CreatedActionMessage;
                    }
                    else
                    {
                        saveTask = UpdateEntityAsync();
                        messageType = MessageType.Changed;
                        messageAction = UpdatedActionMessage;
                    }

                    Result<TDto> result = await saveTask;

                    SendMessage(result.Data, messageType);

                    IsCopy = false;
                    ManualModelChanged = false;

                    SetData(result.Data);

                    SetEditTitle();

                    if (result.Warnings.Any())
                    {
                        MessageFacadeService.ShowNotificationWarning($"{EntityName} №{Model.Id} {messageAction} с предупреждениями");
                        ShowValidationResultView("Предупрежедения", result.Warnings.Select(x => new ValidationResultItem(x, false)).ToList());
                    }
                    else
                    {
                        MessageFacadeService.ShowNotificationInfo($"{EntityName} №{Model.Id} {messageAction} успешно");
                    }

                    processed = true;
                }
                catch (UnexpectedSatusException exception)
                {
                    MessageFacadeService.ShowNotificationError(Resources.ErrorExecutingOperation);
                    ShowValidationResultView("Ошибки при выполнении операции", exception.GetErrorItems());
                }
                catch (UnexpectedErrorException exception)
                {
                    Logger.LogError(exception, "Failed to save entity");
                    ShowValidationResultView(Resources.ServerConnectError, new[] { new ValidationResultItem(Resources.ServerUnavailable, true) });
                }
                catch (Exception exception)
                {
                    Logger.LogError(exception, "Error while saving entity");
                    MessageFacadeService.ShowNotificationError(Resources.ErrorExecutingOperation);
                }
            }
            else
            {
                MessageFacadeService.ShowNotificationWarning("Нечего сохранять");
                processed = true;
            }

            if (LockSupport && IsLockedByCurrentEmployee && AutoUnlock)
            {
                UnlockCommand.Execute(null);
            }

            return processed;
        }

        protected abstract void SetCreateTitle();

        protected void SetData(TDto dto)
        {
            Application.Current.Dispatcher.Invoke(() => SetData((object)dto));
        }

        protected abstract void SetEditTitle();

        protected virtual void SetCopyTitle(int id)
        {
            Title = $"Копирование обьекта №{id}";
        }

        protected abstract Task<Result<TDto>> UpdateEntityAsync();

        protected async Task ExecuteLockableOperationAsync(
            Func<TDto, Task> operation,
            Func<Task<IReadOnlyCollection<ValidationResultItem>>> check)
        {
            LockResponse<TDto> lockResponse = await LockAsync();

            if (lockResponse == null || !lockResponse.Success)
            {
                return;
            }

            bool ok = true;

            if (check != null)
            {
                IReadOnlyCollection<ValidationResultItem> items = await check();

                if (items != null && items.Any())
                {
                    ok = ShowValidationResultView("Ошибки", items);
                }
            }

            if (ok)
            {
                try
                {
                    await operation(lockResponse.Dto);
                }
                catch (UnexpectedErrorException exception)
                {
                    Logger.LogError(exception, "Failed to execute lockable operation");
                    ShowValidationResultView(Resources.ServerConnectError, new[] { new ValidationResultItem(Resources.ServerUnavailable, true) });
                }
                catch (UnexpectedSatusException exception)
                {
                    ShowValidationResultView("Ошибки", exception.GetErrorItems());
                }
                catch (Exception exception)
                {
                    Logger.LogError(exception, "Failed to execute lockable operation");
                    MessageFacadeService.ShowNotificationError(Resources.ErrorExecutingOperation);
                }
            }

            await UnlockAsync();
        }

        protected Task ExecuteLockableOperationAsync(Func<TDto, Task> operation)
        {
            return ExecuteLockableOperationAsync(operation, null);
        }

        protected Task ExecuteLockableOperationAsync(Action<TDto> operation)
        {
            return ExecuteLockableOperationAsync(
                dto =>
                {
                    operation(dto);
                    return Task.CompletedTask;
                });
        }

        protected virtual bool CanEdit()
        {
            return true;
        }

        protected bool CanLock()
        {
            return Model != null && !IsNew && !IsLockedByCurrentEmployee && Model.EmployeeLockId == null && CanEdit();
        }

        private bool CanUnlock()
        {
            return Model != null && !IsNew && IsLockedByCurrentEmployee;
        }

        private async Task<LockResponse<TDto>> LockAsync()
        {
            LockResponse<TDto> response = null;

            try
            {
                response = await LockEntityAsync(Model.Id);

                SetData(response.Dto);

                SendMessage(response.Dto, MessageType.Changed);

                if (!response.Success)
                {
                    MessageFacadeService.ShowNotificationWarning($"Уже заблокировано пользователем {Model.EmployeeLockName}");
                }
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Failed to lock entity");
                MessageFacadeService.ShowNotificationError(Resources.ErrorExecutingOperation);
            }

            return response;
        }

        private async Task<LockResponse<TDto>> UnlockAsync()
        {
            LockResponse<TDto> response = null;

            try
            {
                response = await UnlockEntityAsync(Model.Id);

                SetData(response.Dto);

                SendMessage(response.Dto, MessageType.Changed);

                if (!response.Success)
                {
                    MessageFacadeService.ShowNotificationError("Не удалось разблокировать документ");
                }
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Failed to unlock entity");
                MessageFacadeService.ShowNotificationError("Ошибка при разблокировании");
            }

            return response;
        }

        private void OnModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            RaisePropertiesChanged(nameof(IsChanged), nameof(IsLockedByCurrentEmployee));

            OnModelPropertyChangedInternal(sender, e);
        }

        private void SendMessage(object message)
        {
            Type type = message.GetType();

            MethodInfo methodInfo = typeof(IMessenger).GetMethod("Send", BindingFlags.Public | BindingFlags.Instance)
                .GetGenericMethodDefinition()
                .MakeGenericMethod(type);

            methodInfo.Invoke(Messenger, new[] { message, null, null });
        }

        private void SendMessage(TDto dto, MessageType messageType)
        {
            object entityMessage = CreateEntityMessage(dto, messageType);
            SendMessage(entityMessage);
        }

        private void SetData(object source)
        {
            if (Model != null)
            {
                Model.PropertyChanged -= OnModelPropertyChanged;
            }

            TViewItem model = Mapper.Map<TViewItem>(source);

            if (IsCopy)
            {
                SetCopyTitle(model.Id);
                model.Id = 0;
                ManualModelChanged = true;
            }

            BeforeSetData(model, source);

            ModelOriginal = (TViewItem)model.Clone();

            Model = model;

            compareHelper?.Update(ModelOriginal, Model);

            Model.PropertyChanged += OnModelPropertyChanged;

            RaisePropertiesChanged(nameof(IsChanged), nameof(IsLockedByCurrentEmployee), nameof(IsNew), nameof(IsNewOrIsLockedByCurrentEmployee));

            AfterSetData();
        }
    }
}