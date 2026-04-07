using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using DevExpress.Mvvm;
using DevExpress.Mvvm.UI;
using DevExpress.Utils.Extensions;
using MediatR;
using Microsoft.Extensions.Logging;
using Telemart.Client.Business.Delivery;
using Telemart.Client.Business.Delivery.ScanSheets;
using Telemart.Client.Common.ErrorHandler;
using Telemart.Client.Core.Exceptions;
using Telemart.Client.Data.Requests;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Extensions;
using Telemart.Client.Properties;
using Telemart.Client.TransferObjects;
using Telemart.Client.ViewModels.Base;
using Telemart.Client.ViewModels.Validation;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.ViewModels.Store.CreateScanSheetForEntities
{
    public sealed class SelectEntitiesPageViewModel :
        WizardPageViewModelBase<CreateScanSheetForEntitiesModel>,
        ISupportWizardNextCommand,
        ISupportWizardBackCommand
    {
        public SelectEntitiesPageViewModel(
            IWebClient webClient,
            IMediator mediator,
            ILogger<SelectEntitiesPageViewModel> logger,
            IErrorHandler errorHandler)
        {
            WebClient = webClient;
            Mediator = mediator;
            Logger = logger;
            ErrorHandler = errorHandler;

            HandleLoadedCommand = new AsyncCommand(HandleLoadedAsync);

            HandleUnloadedCommand = new AsyncCommand(HandleUnloadedAsync);
        }

        #region Commands

        public IAsyncCommand HandleLoadedCommand { get; }

        public IAsyncCommand HandleUnloadedCommand { get; }

        #endregion

        public bool CanGoBack => !IsLongOperationInProgress;

        public bool CanGoForward => Model.Entities.Any(x => x.Selected) && !IsLongOperationInProgress;

        public int SelectedCount => Model?.Entities?.Count(x => x.Selected) ?? 0;

        public bool? EntityCheckAllState
        {
            get
            {
                return Model.Entities?.All(r => r.Selected) == true ? true :
                    (Model.Entities?.Any(r => r.Selected) == true ? null : false);
            }

            set
            {
                foreach (ScanSheetEntityViewItem entity in Model.Entities)
                {
                    entity.Selected = value ?? false;
                }
            }
        }

        public override string Description { get; } = "Эти документы попадут в создаваемый реестр";

        public override string Header { get; } = "Шаг 2 - Просмотр документов";

        private IMediator Mediator { get; }

        private IWebClient WebClient { get; }

        private ILogger<SelectEntitiesPageViewModel> Logger { get; }

        private IWizardService WizardService => GetService<IWizardService>();

        private IErrorHandler ErrorHandler { get; }

        private CurrentWindowService CurrentWindowService => (CurrentWindowService)GetService<ICurrentWindowService>(ServiceSearchMode.PreferParents);

        public void OnGoBack(CancelEventArgs e)
        {
        }

        public void OnGoForward(CancelEventArgs e)
        {
            OnGoForwardAsync().ContinueWith(
                _ => WizardService.NavigateToView<CompletePageViewModel>(Model, ((ISupportParentViewModel)this).ParentViewModel),
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

            int[] entityIds = Model.Entities.Where(x => x.Selected).Select(x => x.Id).ToArray();

            try
            {
                IScanSheetProcessor processor = Create(Model.ScanSheetProcessorType.Value);

                bool created = await CreateScanSheetAsync(processor, Model.EntityTypeId, entityIds);

                if (created)
                {
                    Model.ProgressText = GetProgressText(0);

                    IProgress<ProgressInfo> progress = new Progress<ProgressInfo>(x =>
                    {
                        Model.ProgressText = x.Text;
                        Model.ProgressValue = x.Value;
                    });

                    await processor.PrintAsync(Model);
                }
            }
            finally
            {
                IsLongOperationInProgress = false;

                CurrentWindowService.ActualWindow.Closing -= ActualWindowClosing;
            }
        }

        private async Task<bool> CreateScanSheetAsync(IScanSheetProcessor scanSheetProcessor, int entityTypeId, int[] entityIds)
        {
            bool ok = false;

            try
            {
                IRestClientGatewayRequest<Result<ScanSheetCreateResponse[]>> gatewayRequest = scanSheetProcessor.CreateEntityRequest(entityTypeId, entityIds);

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

        private IScanSheetProcessor Create(ScanSheetProcessorType processorType)
        {
            IScanSheetProcessor processor;

            switch (processorType)
            {
                case ScanSheetProcessorType.Novaposhta:
                    processor = new NovaposhtaScanSheetProcessor(Mediator);
                    break;
                default:
                    throw new NotSupportedException();
            }

            return processor;
        }

        private Task HandleLoadedAsync()
        {
            Model.Entities.ForEach(x => x.PropertyChanged += EntityPropertyChanged);
            return Task.CompletedTask;
        }

        private Task HandleUnloadedAsync()
        {
            Model.Entities.ForEach(x => x.PropertyChanged -= EntityPropertyChanged);
            return Task.CompletedTask;
        }

        private void EntityPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ScanSheetEntityViewItem.Selected))
            {
                RaisePropertyChanged(nameof(SelectedCount));
            }
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