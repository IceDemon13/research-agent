using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using DevExpress.Mvvm;
using DevExpress.Mvvm.DataAnnotations;
using DevExpress.Mvvm.Native;
using Microsoft.Extensions.Logging;
using Telemart.Client.Common.Messages;
using Telemart.Client.Common.MvvmEnhancements;
using Telemart.Client.Common.Services;
using Telemart.Client.Core.Exceptions;
using Telemart.Client.Data.Requests.Features.Backlog.Actions;
using Telemart.Client.Data.Requests.Features.Employee;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.Properties;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.Backlog;
using Telemart.Client.TransferObjects.Paging;
using Telemart.Client.ViewModels.Base;
using Telemart.Client.ViewModels.Validation;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.ViewModels.Backlog
{
    public sealed class ChangeResponsibleViewModel : TelemartDialogViewModelBase
    {
        private BacklogTaskDto parameter;

        public ChangeResponsibleViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            IMessenger messenger)
            : base(webClient, dictionaries, messageFacadeService)
        {
            Messenger = messenger;
        }

        public ChangeResponsibleViewModel()
        {
        }

        public ReadOnlyObservableCollection<ComboBoxItem> Employees
        {
            get { return GetProperty(() => Employees); }
            private set { SetProperty(() => Employees, value); }
        }

        public int? EmployeeId
        {
            get { return GetProperty(() => EmployeeId); }
            set { SetProperty(() => EmployeeId, value); }
        }

        private IMessenger Messenger { get; }

        public static void BuildMetadata(MetadataBuilder<ChangeResponsibleViewModel> builder)
        {
        }

        protected override async Task HandleLoadedAsync()
        {
            parameter = (BacklogTaskDto)Parameter;

            PagedResult<EmployeeDto> pagedResult = await WebClient.ExecuteApiRequestAsync(new QueryEmployees(), true);

            IEnumerable<EmployeeDto> employees = pagedResult.Data.Where(x => x.Active || x.Id == WebClient.AuthenticatedEmployee.Id || x.Id == parameter.EmployeeId);

            Employees = employees
                .OrderBy(x => x.Name)
                .Select(x => new ComboBoxItem(x.Id, x.Name))
                .ToReadOnlyObservableCollection();

            EmployeeId = parameter.EmployeeId;

            Title = "Изменение ответственного";
        }

        protected override async Task HandleOkAsync()
        {
            if (IDataErrorInfoHelper.HasErrors(this))
            {
                return;
            }

            try
            {
                Result<BacklogTaskDto> result = await WebClient.ExecuteApiRequestAsync(new SetBacklogTaskResponsible(parameter.Id, EmployeeId));

                Messenger.Send(new BacklogTaskMessage(result.Data, MessageType.Changed));

                if (result.Warnings.Any())
                {
                    MessageFacadeService.ShowNotificationWarning($"Ответственный задачи №{result.Data.Id} изменен с предупреждениями");
                    ShowValidationResultView("Предупрежедения", result.Warnings.Select(x => new ValidationResultItem(x, false)).ToList());
                }
                else
                {
                    MessageFacadeService.ShowNotificationInfo($"Ответственный задачи №{result.Data.Id} успешно изменен");
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
                Logger.LogError(exception, "Failed to change task responsible");
                ShowValidationResultView(Resources.ServerConnectError, new[] { new ValidationResultItem(Resources.ServerUnavailable, true) });
            }
            catch (Exception exception)
            {
                MessageFacadeService.ShowNotificationError("Ошибка при изменении задачи");
                Logger.LogError(exception, "Error while changing task responsible");
            }
        }
    }
}