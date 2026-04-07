using System;
using System.Diagnostics;
using System.Net;
using System.Threading.Tasks;
using DevExpress.Mvvm;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Telemart.Client.Common.Services;
using Telemart.Client.Core.Exceptions;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.Properties;
using Telemart.Client.ViewModels.Common;

namespace Telemart.Client.ViewModels.Base
{
    public abstract class TelemartViewModelBase : ViewModelBase
    {
        public static IServiceProvider ServiceProvider { get; set; }

        protected TelemartViewModelBase(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            ILogger logger = null)
        {
            WebClient = webClient ?? throw new ArgumentNullException(nameof(webClient));
            Dictionaries = dictionaries ?? throw new ArgumentNullException(nameof(dictionaries));
            MessageFacadeService = messageFacadeService ?? throw new ArgumentNullException(nameof(messageFacadeService));

            // TODO: Change to constructor injection
            Logger = logger ?? ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger(GetType());

            HandleLoadedCommand = new AsyncCommand(HandleLoadedInternalAsync);
            HandleUnloadedCommand = new AsyncCommand(HandleUnloadedInternalAsync);
        }

        protected TelemartViewModelBase()
        {
        }

        public bool Loaded
        {
            get { return GetProperty(() => Loaded); }
            protected set { SetProperty(() => Loaded, value); }
        }

        public IAsyncCommand HandleLoadedCommand { get; }

        public IAsyncCommand HandleUnloadedCommand { get; }

        protected IDictionaries Dictionaries { get; }

        protected ILogger Logger { get; }

        protected IMessageFacadeService MessageFacadeService { get; }

        protected IWebClient WebClient { get; }

        protected virtual Task HandleLoadedAsync()
        {
            return Task.CompletedTask;
        }

        protected virtual Task HandleUnloadedAsync()
        {
            return Task.CompletedTask;
        }

        protected virtual void OnHandleLoadedFinished(string errorMessage = null)
        {
            if (!string.IsNullOrWhiteSpace(errorMessage))
            {
                MessageFacadeService.ShowNotificationError(errorMessage);
            }
        }

        protected virtual void OnHandleLoadedStarted()
        {
        }

        protected override void OnInitializeInDesignMode()
        {
        }

        protected virtual void OnHandleUnloadedStarted()
        {
        }

        protected virtual bool SubmitUsageReason(IDocumentManagerService documentManagerService, string featureMethodName, int? entityId)
        {
            UsageReasonViewModel usageReasonViewModel = documentManagerService.ShowView<UsageReasonViewModel>(
                new UsageReasonParameter(entityId, $"{GetType().Name}.{featureMethodName}"),
                this);

            return usageReasonViewModel.IsOk;
        }

        private static string GetUnexpectedStatusErrorMessage(UnexpectedSatusException exception)
        {
            string error;

            switch (exception.Args.HttpStatusCode)
            {
                case HttpStatusCode.Forbidden:
                    error = Resources.ErrorForbidden;
                    break;
                default:
                    error = Resources.ErrorDuringDataLoading;
                    break;
            }

            return error;
        }

        private async Task HandleLoadedInternalAsync()
        {
            Stopwatch stopwatch = Stopwatch.StartNew();

            try
            {
                Loaded = false;

                OnHandleLoadedStarted();
                await HandleLoadedAsync();
                OnHandleLoadedFinished();

                Loaded = true;
            }
            catch (UnexpectedSatusException exception)
            {
                Logger.LogError(exception, "Failed to load {ViewModelName}", GetType().Name);
                OnHandleLoadedFinished(GetUnexpectedStatusErrorMessage(exception));
            }
            catch (UnexpectedErrorException exception)
            {
                Logger.LogError(exception, "Failed to load {ViewModelName}", GetType().Name);
                OnHandleLoadedFinished(Resources.ServerConnectError);
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Failed to load {ViewModelName}", GetType().Name);
                OnHandleLoadedFinished("Непредвиденная ошибка");
            }
            finally
            {
                stopwatch.Stop();
                Logger.LogInformation("{ViewModelName} loaded in {LoadingTime}ms", GetType().Name, stopwatch.ElapsedMilliseconds);
            }
        }

        private async Task HandleUnloadedInternalAsync()
        {
            Stopwatch stopwatch = Stopwatch.StartNew();

            try
            {
                Loaded = false;

                OnHandleUnloadedStarted();
                await HandleUnloadedAsync();

                Loaded = true;
            }
            catch (UnexpectedSatusException exception)
            {
                Logger.LogError(exception, "Failed to unload {ViewModelName}", GetType().Name);
                OnHandleLoadedFinished(GetUnexpectedStatusErrorMessage(exception));
            }
            catch (UnexpectedErrorException exception)
            {
                Logger.LogError(exception, "Failed to unload {ViewModelName}", GetType().Name);
                OnHandleLoadedFinished(Resources.ServerConnectError);
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Failed to unload {ViewModelName}", GetType().Name);
                OnHandleLoadedFinished("Непредвиденная ошибка");
            }
            finally
            {
                stopwatch.Stop();
                Logger.LogInformation("{ViewModelName} unloaded in {LoadingTime}ms", GetType().Name, stopwatch.ElapsedMilliseconds);
            }
        }
    }
}