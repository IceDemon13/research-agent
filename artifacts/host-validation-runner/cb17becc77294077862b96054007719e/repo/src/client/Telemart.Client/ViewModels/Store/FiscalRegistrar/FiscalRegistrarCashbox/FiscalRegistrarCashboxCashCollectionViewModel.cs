using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DevExpress.Mvvm;
using DevExpress.Mvvm.DataAnnotations;
using Telemart.Client.Common.ErrorHandler;
using Telemart.Client.Common.Services;
using Telemart.Client.Data.Requests.Features.Cashbox;
using Telemart.Client.Data.Requests.Features.Cashbox.Actions;
using Telemart.Client.Data.Requests.Features.FiscalDocument;
using Telemart.Client.Data.Requests.Features.Refund;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.FiscalRegistrar;
using Telemart.Client.FiscalRegistrar.Abstraction;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.Cashbox;
using Telemart.Client.ViewModels.Base;
using Telemart.Client.ViewModels.Money.Refund;
using Telemart.Common.ErrorHandling;
using Telemart.Fiscal.Client.TransferObjects;

namespace Telemart.Client.ViewModels.Store.FiscalRegistrar.FiscalRegistrarCashbox
{
    public class FiscalRegistrarCashboxCashCollectionViewModel : TelemartDialogViewModelBase
    {
        private int cashboxId;

        public FiscalRegistrarCashboxCashCollectionViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            IFiscalRegistrarClientFactory fiscalRegistrarClientFactory,
            IErrorHandler errorHandler)
            : base(webClient, dictionaries, messageFacadeService)
        {
            FiscalRegistrarClientFactory = fiscalRegistrarClientFactory;
            ErrorHandler = errorHandler;
        }

        public decimal InCashbox
        {
            get { return GetProperty(() => InCashbox); }
            private set { SetProperty(() => InCashbox, value, () => RaisePropertyChanged(nameof(Remain))); }
        }

        public decimal Collect
        {
            get { return GetProperty(() => Collect); }
            set { SetProperty(() => Collect, value, () => RaisePropertyChanged(nameof(Remain))); }
        }

        public decimal Remain => InCashbox - Collect;

        public decimal Incombustible
        {
            get { return GetProperty(() => Incombustible); }
            private set { SetProperty(() => Incombustible, value); }
        }

        public decimal RefundsAmount
        {
            get { return GetProperty(() => RefundsAmount); }
            private set { SetProperty(() => RefundsAmount, value); }
        }

        private IFiscalRegistrarClientFactory FiscalRegistrarClientFactory { get; }

        private IErrorHandler ErrorHandler { get; }

        public static void BuildMetadata(MetadataBuilder<FiscalRegistrarCashboxCashCollectionViewModel> builder)
        {
            builder.Property(x => x.Collect)
                .MatchesInstanceRule((x, y) => y.InCashbox - x >= y.Incombustible, () => "В кассе должна остаться сумма не меньше, чем несгораемый остаток");

            builder.Property(x => x.Collect)
                .MatchesInstanceRule((x, y) => y.InCashbox >= x, () => "Нельзя извлечь большую сумму, чем сумма в кассе");
        }

        protected override async Task HandleLoadedAsync()
        {
            if (IDataErrorInfoHelper.HasErrors(this))
            {
                return;
            }

            Result<IFiscalRegistrarClient> clientResult = await ErrorHandler.HandleErrorsAsync(
                ct => FiscalRegistrarClientFactory.CreateAsync(ct),
                "Определении настроек РРО",
                null,
                this,
                true);

            if (!clientResult.IsSuccess)
            {
                return;
            }

            cashboxId = clientResult.Data.Settings.CashboxId;

            CashboxDto cashbox = await WebClient.ExecuteApiRequestAsync(new QueryCashbox(cashboxId));

            if (cashbox?.IncombustibleAmount == null)
            {
                MessageFacadeService.ShowNotificationWarning("У кассы должен быть заполнен несгораемый остаток");
                Close();
                return;
            }

            Incombustible = cashbox.IncombustibleAmount.Value;

            IFilteringItem filteringItem = new RefundsFilteringItem()
            {
                States = new[] { RefundState.Confirmed.Id },
                Cashboxes = new[] { cashboxId }
            };

            List<RefundDto> refunds = await WebClient.ExecuteApiRequestAsync(new QueryRefunds(filteringItem)).GetPagedResultDataAsync();

            RefundsAmount = refunds.Sum(x => x.Amount);

            if (clientResult.IsSuccess)
            {
                await ErrorHandler.HandleErrorsAsync(
                    ct => clientResult.Data.GetCashStockAsync(cashboxId, ct),
                    "запросе остатка по кассе из РРО",
                    "Запрос остатка суммы в кассе выполнен",
                    this,
                    true,
                    onSuccess: async (r, ct) =>
                    {
                        InCashbox = r;

                        Collect = Math.Max(0, InCashbox - Incombustible - RefundsAmount);

                        await base.HandleLoadedAsync();

                        Title = "Изъятие из кассы РРО";
                    },
                    onError: (_, _) =>
                    {
                        Close();
                        return Task.CompletedTask;
                    });
            }
        }

        protected override async Task HandleOkAsync()
        {
            Result<IFiscalRegistrarClient> clientResult = await ErrorHandler.HandleErrorsAsync(
                ct => FiscalRegistrarClientFactory.CreateAsync(ct),
                "Определении настроек РРО",
                null,
                this,
                true);

            if (!clientResult.IsSuccess)
            {
                return;
            }

            bool getStateResult = await ErrorHandler.HandleErrorsAsync(
                ct => clientResult.Data.GetStateAsync(cashboxId, ct),
                "проверке статуса РРО",
                "Проверка статуса РРО прошла",
                this,
                true);

            if (!getStateResult)
            {
                return;
            }

            await ErrorHandler.HandleErrorsAsync(
                async ct =>
                {
                    await WebClient.ExecuteApiRequestAsync(new CashboxCashCollection(cashboxId, Collect));
                    return await clientResult.Data.CashCollectionAsync(cashboxId, Collect, ct);
                },
                "инкассации",
                "Инкассация выполнена",
                this,
                true,
                onSuccess: async (r, _) =>
                {
                    FiscalDocumentCreateDto fiscalDocumentCreateDto = new FiscalDocumentCreateDto()
                    {
                        Amount = -Collect,
                        CashboxId = cashboxId,
                        Number = r.Id,
                        Real = true,
                        TypeId = FiscalDocumentType.CashCollection.Id,
                        Payments = new[] { new FiscalDocumentPaymentDto { TypeId = FiscalPaymentType.CashPayment.Id, Amount = -Collect } }
                    };

                    await WebClient.ExecuteApiRequestAsync(new CreateFiscalDocument(fiscalDocumentCreateDto));

                    IsOk = true;
                    Close();
                });
        }
    }
}