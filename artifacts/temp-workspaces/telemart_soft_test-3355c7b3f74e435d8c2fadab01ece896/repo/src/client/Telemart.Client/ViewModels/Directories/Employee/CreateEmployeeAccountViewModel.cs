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
using Telemart.Client.Data.Requests.Features.Employee.Account;
using Telemart.Client.Data.Requests.Features.WorkAccount;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.Properties;
using Telemart.Client.TransferObjects.WorkAccount;
using Telemart.Client.ViewModels.Base;
using Telemart.Client.ViewModels.Validation;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.ViewModels.Directories.Employee
{
    public class CreateEmployeeAccountViewModel : TelemartDialogViewModelBase
    {
        private int employeeId;

        public CreateEmployeeAccountViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            IMessenger messenger)
        : base(webClient, dictionaries, messageFacadeService)
        {
            Messenger = messenger;
        }

        public CreateEmployeeAccountViewModel()
        {
        }

        public ReadOnlyObservableCollection<ComboBoxItem> WorkAccounts
        {
            get { return GetProperty(() => WorkAccounts); }
            private set { SetProperty(() => WorkAccounts, value); }
        }

        public int? AccountId
        {
            get { return GetProperty(() => AccountId); }
            set { SetProperty(() => AccountId, value); }
        }

        public string Login
        {
            get { return GetProperty(() => Login); }
            set { SetProperty(() => Login, value); }
        }

        public string Password
        {
            get { return GetProperty(() => Password); }
            set { SetProperty(() => Password, value); }
        }

        private IMessenger Messenger { get; }

        public static void BuildMetadata(MetadataBuilder<CreateEmployeeAccountViewModel> builder)
        {
            builder.Property(x => x.AccountId).Required(() => Resources.RequiredErrorMessage);
            builder.Property(x => x.Login).Required(() => Resources.RequiredErrorMessage);
        }

        protected override async Task HandleLoadedAsync()
        {
            employeeId = (int)Parameter;

            List<WorkAccountDto> accounts = await WebClient.ExecuteApiRequestAsync(new QueryWorkAccounts(), true);

            WorkAccounts = accounts
                .Select(x => new ComboBoxItem(x.Id, x.Name))
                .OrderBy(x => x.DisplayValue)
                .ToReadOnlyObservableCollection();

            Title = "Добавление аккаунта";
        }

        protected override async Task HandleOkAsync()
        {
            try
            {
                EmployeeAccountCreateDto dto = new EmployeeAccountCreateDto
                {
                    AccountId = AccountId!.Value,
                    Login = Login,
                    Password = Password
                };

                Result<EmployeeAccountDto> result = await WebClient.ExecuteApiRequestAsync(new CreateEmployeeAccount(employeeId, dto));

                Messenger.Send(new EmployeeAccountMessage(result.Data, MessageType.Added));

                Close();
            }
            catch (UnexpectedSatusException exception)
            {
                MessageFacadeService.ShowNotificationError("Ошибка при добавлении аккаунта пользователю");
                ShowValidationResultView("Ошибки при добавлении аккаунта пользователю", exception.GetErrorItems());
            }
            catch (UnexpectedErrorException exception)
            {
                Logger.LogError(exception, "Failed to add account to employee");
                ShowValidationResultView(Resources.ServerConnectError, new[] { new ValidationResultItem(Resources.ServerUnavailable, true) });
            }
            catch (Exception exception)
            {
                MessageFacadeService.ShowNotificationError("Ошибка при добавлении аккаунта пользователю");
                Logger.LogError(exception, "Error while adding account to employee");
            }
        }
    }
}