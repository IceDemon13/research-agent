using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using Microsoft.Extensions.Logging;
using Telemart.Client.Common.Services;
using Telemart.Client.Core.Exceptions;
using Telemart.Client.Data.Requests;
using Telemart.Client.Data.Requests.Features.Bank;
using Telemart.Client.Data.Requests.Features.Cashbox;
using Telemart.Client.Data.Requests.Features.Organization;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.Properties;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.Cashbox;
using Telemart.Client.TransferObjects.Paging;
using Telemart.Client.ViewModels.Base;
using Telemart.Client.ViewModels.Validation;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.ViewModels.Directories.Organization
{
    public sealed class OrganizationAccountViewModel : TelemartDialogViewModelBase
    {
        private List<CashboxDto> cashboxes;

        public OrganizationAccountViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            IMapper mapper)
            : base(webClient, dictionaries, messageFacadeService)
        {
            Mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));

            HandleCurrencyChangedCommand = new DelegateCommand(HandleCurrencyChanged);
        }

        public OrganizationAccountViewModel()
        {
        }

        #region Commands

        public IDelegateCommand HandleCurrencyChangedCommand { get; }

        #endregion

        #region INPC

        public OrganizationAccountViewItem ResultItem
        {
            get { return GetProperty(() => ResultItem); }
            private set { SetProperty(() => ResultItem, value); }
        }

        public OrganizationAccountViewItem Model
        {
            get { return GetProperty(() => Model); }
            private set { SetProperty(() => Model, value); }
        }

        public ReadOnlyObservableCollection<BankDto> Banks
        {
            get { return GetProperty(() => Banks); }
            private set { SetProperty(() => Banks, value); }
        }

        public ReadOnlyObservableCollection<Currency> Currencies
        {
            get { return GetProperty(() => Currencies); }
            private set { SetProperty(() => Currencies, value); }
        }

        public ReadOnlyObservableCollection<CashboxDto> Cashboxes
        {
            get { return GetProperty(() => Cashboxes); }
            private set { SetProperty(() => Cashboxes, value); }
        }

        public ReadOnlyObservableCollection<Payment> Payments
        {
            get { return GetProperty(() => Payments); }
            private set { SetProperty(() => Payments, value); }
        }

        public bool IsNew
        {
            get { return GetProperty(() => IsNew); }
            private set { SetProperty(() => IsNew, value); }
        }

        #endregion INPC

        private IMapper Mapper { get; }

        protected override async Task HandleLoadedAsync()
        {
            object[] p = (object[])Parameter;

            OrganizationAccountViewItem viewItem = (OrganizationAccountViewItem)p[0];
            bool isOrganizationVatPayer = (bool)p[1];

            Currencies = new[] { Currency.Uah, Currency.Usd, Currency.Eur }.ToReadOnlyObservableCollection();

            int[] paymentIds = isOrganizationVatPayer
                ? new[] { Payment.CashlessTaxId }
                : new[] { Payment.BankId, Payment.WmuId, Payment.CashlessNoTaxId };

            Payments = Dictionaries.GetItems<Payment>().Where(x => paymentIds.Contains(x.Id)).ToReadOnlyObservableCollection();

            PagedResult<BankDto> banks = await WebClient.ExecuteApiRequestAsync(new QueryBanks(), true);
            cashboxes = await WebClient.ExecuteApiRequestAsync(new QueryCashboxes(), true);

            Banks = banks.Data.ToReadOnlyObservableCollection();

            Cashboxes = GetCashboxes(cashboxes, viewItem.CashboxId);

            IsNew = viewItem.Id == 0;
            Model = viewItem;

            Title = IsNew
                ? "Создание счета"
                : "Изменение счета";
        }

        protected override async Task HandleOkAsync()
        {
            try
            {
                IRestClientGatewayRequest<Result<OrganizationAccountDto>> gatewayRequest;

                if (IsNew)
                {
                    gatewayRequest = new CreateOrganizationAccount(Model.OrganizationId, Mapper.Map<OrganizationAccountDto>(Model));
                }
                else
                {
                    gatewayRequest = new UpdateOrganizationAccount(Model.OrganizationId, Mapper.Map<OrganizationAccountSaveDto>(Model));
                }

                Result<OrganizationAccountDto> result = await WebClient.ExecuteApiRequestAsync(gatewayRequest);

                ResultItem = Mapper.Map(result.Data, OrganizationAccountViewItem.Create());

                MessageFacadeService.ShowNotificationInfo("Счет успешно сохранен");

                IsOk = true;
                Close();
            }
            catch (UnexpectedSatusException exception)
            {
                MessageFacadeService.ShowNotificationError("Ошибка при сохранении счета");
                ShowValidationResultView("Ошибки при сохранении счета", exception.GetErrorItems());
            }
            catch (UnexpectedErrorException exception)
            {
                Logger.LogError(exception, "Failed to save organization account");
                MessageFacadeService.ShowNotificationError("Ошибка при сохранении счета");
                ShowValidationResultView("Ошибки при сохранении счета", new[] { new ValidationResultItem(Resources.ServerUnavailable, true) });
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Failed to save organization account");
                MessageFacadeService.ShowNotificationError("Ошибка при сохранении счета");
            }
        }

        private static ReadOnlyObservableCollection<CashboxDto> GetCashboxes(
            IEnumerable<CashboxDto> cashboxes,
            int currentCashboxId,
            int? cashboxCurrency = null)
        {
            return cashboxes
                .Where(x => (x.IsActive || x.Id == currentCashboxId) && (cashboxCurrency == null || x.CurrencyId == cashboxCurrency.Value))
                .OrderBy(x => x.Name)
                .ToReadOnlyObservableCollection();
        }

        private void HandleCurrencyChanged()
        {
            Cashboxes = GetCashboxes(cashboxes, Model.CashboxId, Model.CurrencyId);
        }
    }
}