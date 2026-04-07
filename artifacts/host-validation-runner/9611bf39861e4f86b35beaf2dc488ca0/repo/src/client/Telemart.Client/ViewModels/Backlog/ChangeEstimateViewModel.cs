using System;
using System.Linq;
using System.Threading.Tasks;
using DevExpress.Mvvm;
using DevExpress.Mvvm.DataAnnotations;
using Microsoft.Extensions.Logging;
using Telemart.Client.Common.Messages;
using Telemart.Client.Common.Services;
using Telemart.Client.Core.Exceptions;
using Telemart.Client.Data.Requests.Features.Backlog.Actions;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.Properties;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.Backlog;
using Telemart.Client.ViewModels.Base;
using Telemart.Client.ViewModels.Validation;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.ViewModels.Backlog
{
    public sealed class ChangeEstimateViewModel : TelemartDialogViewModelBase
    {
        private BacklogTaskDto parameter;

        public ChangeEstimateViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            IMessenger messenger)
            : base(webClient, dictionaries, messageFacadeService)
        {
            Messenger = messenger;
        }

        public ChangeEstimateViewModel()
        {
        }

        public int? Estimate
        {
            get { return GetProperty(() => Estimate); }
            set { SetProperty(() => Estimate, value); }
        }

        private IMessenger Messenger { get; }

        public static void BuildMetadata(MetadataBuilder<ChangeEstimateViewModel> builder)
        {
            builder.Property(x => x.Estimate)
                .MatchesRegularExpression("[1-9][0-9]*", () => "Число должно быть больше либо равно 1");
        }

        protected override Task HandleLoadedAsync()
        {
            parameter = (BacklogTaskDto)Parameter;

            Estimate = parameter.Estimate;

            Title = "Изменить estimate";

            return Task.CompletedTask;
        }

        protected override async Task HandleOkAsync()
        {
            if (IDataErrorInfoHelper.HasErrors(this))
            {
                return;
            }

            try
            {
                Result<BacklogTaskDto> result = await WebClient.ExecuteApiRequestAsync(new SetBacklogTaskEstimate(parameter.Id, Estimate));

                Messenger.Send(new BacklogTaskMessage(result.Data, MessageType.Changed));

                if (result.Warnings.Any())
                {
                    MessageFacadeService.ShowNotificationWarning($"Estimate задачи №{result.Data.Id} изменен с предупреждениями");
                    ShowValidationResultView("Предупрежедения", result.Warnings.Select(x => new ValidationResultItem(x, false)).ToList());
                }
                else
                {
                    MessageFacadeService.ShowNotificationInfo($"Estimate задачи №{result.Data.Id} успешно изменен");
                }

                IsOk = true;
                Close();
            }
            catch (UnexpectedSatusException exception)
            {
                MessageFacadeService.ShowNotificationError("Ошибка при изменении задачи");
                ShowValidationResultView("Ошибки при изменении задачи", exception.GetErrorItems());
            }
            catch (UnexpectedErrorException exception)
            {
                Logger.LogError(exception, "Failed to change task estimate");
                ShowValidationResultView(Resources.ServerConnectError, new[] { new ValidationResultItem(Resources.ServerUnavailable, true) });
            }
            catch (Exception exception)
            {
                MessageFacadeService.ShowNotificationError("Ошибка при изменении задачи");
                Logger.LogError(exception, "Error while changing task estimate");
            }
        }
    }
}