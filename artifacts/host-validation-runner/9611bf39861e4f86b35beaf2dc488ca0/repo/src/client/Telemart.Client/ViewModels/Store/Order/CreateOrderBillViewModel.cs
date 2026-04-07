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
using Telemart.Client.Common.Services;
using Telemart.Client.Core.Exceptions;
using Telemart.Client.Data.Requests.Features.OrderBill;
using Telemart.Client.Data.Requests.Features.Organization;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.Properties;
using Telemart.Client.TransferObjects;
using Telemart.Client.ViewModels.Base;
using Telemart.Client.ViewModels.Validation;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.ViewModels.Store.Order
{
    public sealed class CreateOrderBillViewModel : TelemartDialogViewModelBase
    {
        private readonly IMessenger messenger;
        private CreateOrderBillParameter parameter;

        public CreateOrderBillViewModel(IWebClient webClient, IDictionaries dictionaries, IMessageFacadeService messageFacadeService, IMessenger messenger)
           : base(webClient, dictionaries, messageFacadeService)
        {
            this.messenger = messenger;

            ExpireDate = DateTime.Now + TimeSpan.FromDays(3);
        }

        public ObservableCollection<OrganizationDto> Organizations
        {
            get { return GetProperty(() => Organizations); }
            private set { SetProperty(() => Organizations, value); }
        }

        public OrganizationDto SelectedOrganization
        {
            get { return GetProperty(() => SelectedOrganization); }
            set { SetProperty(() => SelectedOrganization, value, SelectedOrganizationChanged); }
        }

        public ObservableCollection<OrganizationAccountDto> SelectedOrganizationAccounts
        {
            get { return GetProperty(() => SelectedOrganizationAccounts); }
            set { SetProperty(() => SelectedOrganizationAccounts, value); }
        }

        public OrganizationAccountDto SelectedOrganizationAccount
        {
            get { return GetProperty(() => SelectedOrganizationAccount); }
            set { SetProperty(() => SelectedOrganizationAccount, value); }
        }

        public DateTime? ExpireDate
        {
            get { return GetProperty(() => ExpireDate); }
            set { SetProperty(() => ExpireDate, value); }
        }

        public bool SelectedOrganizationAccountsEnabled
        {
            get { return GetProperty(() => SelectedOrganizationAccountsEnabled); }
            set { SetProperty(() => SelectedOrganizationAccountsEnabled, value); }
        }

        public static void BuildMetadata(MetadataBuilder<CreateOrderBillViewModel> builder)
        {
            builder.Property(x => x.SelectedOrganization)
               .Required(() => Resources.RequiredErrorMessage);

            builder.Property(x => x.SelectedOrganizationAccount)
                .Required(() => Resources.RequiredErrorMessage);

            builder.Property(x => x.ExpireDate)
              .Required(() => Resources.RequiredErrorMessage);
        }

        protected override async Task HandleLoadedAsync()
        {
            parameter = (CreateOrderBillParameter)Parameter;

            List<OrganizationDto> organizations = await WebClient.ExecuteApiRequestAsync(new QueryOrganizations()).GetPagedResultDataAsync();

            organizations.ForEach(x => x.Accounts = x.Accounts.Where(x => x.Payments.Contains(parameter.OrderPaymentId)).ToList());

            Organizations = organizations.Where(x => x.Accounts.Any() && x.Active).ToObservableCollection();

            Title = "Создание счета";

            await base.HandleLoadedAsync();
        }

        protected override async Task HandleOkAsync()
        {
            const string ErrorRu = "Ошибка при создании счета";
            const string ErrorEn = "Error while creating order bill";

            try
            {
                CreateOrderBillDto createDto = new CreateOrderBillDto()
                {
                    OrderId = parameter.OrderId,
                    ExpireDate = ExpireDate,
                    OrganizationAccountId = SelectedOrganizationAccount.Id
                };

                Result<OrderBillDto> result = await WebClient.ExecuteApiRequestAsync(new CreateOrderBill(createDto));
                messenger.Send(new OrderBillMessage(result.Data, MessageType.Added));
                MessageFacadeService.ShowNotificationInfo("Счет успешно создан");

                IsOk = true;
                Close();
            }
            catch (UnexpectedSatusException exception)
            {
                ShowValidationResultView(ErrorRu, exception.GetErrorItems());
            }
            catch (UnexpectedErrorException exception)
            {
                Logger.LogError(exception, ErrorEn);
                ShowValidationResultView(Resources.ServerConnectError, new[] { new ValidationResultItem(Resources.ServerUnavailable, true) });
            }
            catch (Exception exception)
            {
                MessageFacadeService.ShowNotificationError(ErrorRu);
                Logger.LogError(exception, ErrorEn);
            }
        }

        private void SelectedOrganizationChanged()
        {
            if (SelectedOrganization is null)
            {
                SelectedOrganizationAccounts.Clear();
                SelectedOrganizationAccount = null;
                SelectedOrganizationAccountsEnabled = false;
            }
            else
            {
                SelectedOrganizationAccounts = SelectedOrganization.Accounts.ToObservableCollection();
                SelectedOrganizationAccountsEnabled = true;
            }
        }
    }
}