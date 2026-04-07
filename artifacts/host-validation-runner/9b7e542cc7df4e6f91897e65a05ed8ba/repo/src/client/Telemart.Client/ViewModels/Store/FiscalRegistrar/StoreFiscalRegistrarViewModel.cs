using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DevExpress.Mvvm;
using DevExpress.Mvvm.DataAnnotations;
using DevExpress.Mvvm.Native;
using Microsoft.Extensions.Logging;
using Telemart.Client.Common;
using Telemart.Client.Common.ErrorHandler;
using Telemart.Client.Common.MvvmEnhancements;
using Telemart.Client.Common.Services;
using Telemart.Client.Core.Exceptions;
using Telemart.Client.Data.Requests.Features.Cashbox;
using Telemart.Client.Data.Requests.Features.FiscalDocument;
using Telemart.Client.Data.Requests.Features.Order;
using Telemart.Client.Data.Requests.Features.Products;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.FiscalRegistrar;
using Telemart.Client.FiscalRegistrar.Abstraction;
using Telemart.Client.FiscalRegistrar.Entities;
using Telemart.Client.Properties;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.Cashbox;
using Telemart.Client.TransferObjects.FiscalDocument;
using Telemart.Client.TransferObjects.Paging;
using Telemart.Client.ViewModels.Base;
using Telemart.Client.ViewModels.Common;
using Telemart.Client.ViewModels.Nomenclature;
using Telemart.Client.ViewModels.Store.FiscalRegistrar.FiscalRegistrarCashbox;
using Telemart.Common.ErrorHandling;
using Telemart.Common.Localization;
using Telemart.Fiscal.Client.Extensions;
using Telemart.Fiscal.Client.TransferObjects;
using ProductType = Telemart.Common.Dictionaries.ProductType;

namespace Telemart.Client.ViewModels.Store.FiscalRegistrar
{
    public sealed class StoreFiscalRegistrarViewModel : TelemartDialogViewModelBase
    {
        public StoreFiscalRegistrarViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            IFiscalRegistrarClientFactory fiscalRegistrarClientFactory,
            IErrorHandler errorHandler)
            : base(webClient, dictionaries, messageFacadeService)
        {
            FiscalRegistrarClientFactory = fiscalRegistrarClientFactory;
            ErrorHandler = errorHandler;

            AddCommand = new AsyncCommand(AddAsync);
            AddAllFromOrderCommand = new AsyncCommand(AddAllFromOrderAsync, () => OrderId.HasValue && OrderId > 0);
            RemoveCommand = new DelegateCommand<StoreFiscalRegistrarViewItem>(Remove, x => x != null);
            HandleProductPropertyChangedCommand = new DelegateCommand(HandleProductPropertyChanged);
            CashboxOperationsCommand = new DelegateCommand(CashboxOperations);
            PrintChequeCommand = new AsyncCommand<ChequeType>(PrintChequeAsync);
        }

        public StoreFiscalRegistrarViewModel()
        {
        }

        #region Commands

        public IAsyncCommand AddCommand { get; }

        public IAsyncCommand AddAllFromOrderCommand { get; }

        public IDelegateCommand RemoveCommand { get; }

        public IDelegateCommand HandleProductPropertyChangedCommand { get; }

        public IDelegateCommand CashboxOperationsCommand { get; }

        public IAsyncCommand PrintChequeCommand { get; }

        #endregion

        public ObservableCollection<StoreFiscalRegistrarViewItem> NomenclatureItems
        {
            get { return GetProperty(() => NomenclatureItems); }
            private set { SetProperty(() => NomenclatureItems, value); }
        }

        public ReadOnlyObservableCollection<ComboBoxItem> PaymentTypes
        {
            get { return GetProperty(() => PaymentTypes); }
            private set { SetProperty(() => PaymentTypes, value); }
        }

        public int? OrderId
        {
            get { return GetProperty(() => OrderId); }
            set { SetProperty(() => OrderId, value); }
        }

        public decimal ToPayAmount
        {
            get { return GetProperty(() => ToPayAmount); }
            set { SetProperty(() => ToPayAmount, value, () => { RaisePropertiesChanged(nameof(Amount), nameof(ShortChange)); }); }
        }

        public decimal Amount
        {
            get { return GetProperty(() => Amount); }
            set { SetProperty(() => Amount, value, () => { RaisePropertiesChanged(nameof(ShortChange)); }); }
        }

        public int PaymentTypeId
        {
            get { return GetProperty(() => PaymentTypeId); }
            set { SetProperty(() => PaymentTypeId, value); }
        }

        public decimal ShortChange => Amount - ToPayAmount;

        #region DialogSettings

        public override int Height => 450;

        public override int MinHeight => 338;

        public override int MinWidth => 600;

        public override int Width => 800;

        #endregion

        private IFiscalRegistrarClientFactory FiscalRegistrarClientFactory { get; }

        private IErrorHandler ErrorHandler { get; }

        public static void BuildMetadata(MetadataBuilder<StoreFiscalRegistrarViewModel> builder)
        {
            builder.Property(x => x.OrderId)
                .MatchesRule(x => x > 0, () => Resources.RequiredErrorMessage);

            builder.Property(x => x.ToPayAmount)
                .MatchesRule(x => x > 0, () => Resources.RequiredErrorMessage);

            builder.Property(x => x.Amount)
                .MatchesRule(x => x > 0, () => Resources.RequiredErrorMessage)
                .MatchesInstanceRule((x, y) => x >= y.ToPayAmount, () => "Значение не может быть меньше чем сумма к оплате");
        }

        protected override async Task HandleLoadedAsync()
        {
            await Task.WhenAll(RefreshPaymentTypesAsync());

            NomenclatureItems = new ObservableCollection<StoreFiscalRegistrarViewItem>();
            Title = "Провести через РРО";
        }

        protected override bool CanOk()
        {
            return !HasErrors();
        }

        protected override Task HandleOkAsync()
        {
            throw new NotSupportedException();
        }

        private bool HasErrors()
        {
            return IDataErrorInfoHelper.HasErrors(this, 1) || NomenclatureItems.Any(x => IDataErrorInfoHelper.HasErrors(x, 1));
        }

        private async Task AddAsync()
        {
            NomenclatureViewOptions options = new NomenclatureViewOptions(
                NomenclatureViewPriceContext.Client,
                Constants.TelemartContractorId,
                NomenclatureViewSelectionMode.ByQuantity);

            NomenclatureViewModel viewModel = SizeableDialogDocumentManagerService.ShowView<NomenclatureViewModel>(options, this);

            PagedResult<ProductAttributesDto> barcodeAttributes = await WebClient.ExecuteApiRequestAsync(new QueryProductsAttributesByIds(viewModel.GetSelectedItems().Select(x => x.Id).ToArray(), false));

            if (viewModel.IsOk)
            {
                NomenclatureItems.AddRange(viewModel.GetSelectedItems().Select(x => new StoreFiscalRegistrarViewItem
                {
                    Id = x.Id,
                    NameFullUa = x.NameFullUa,
                    Barcode = barcodeAttributes.Data.FirstOrDefault(z => z.ProductId == x.Id)?.Barcodes?.FirstOrDefault(z => z.FiscalRegistrar)?.Barcode,
                    Quantity = x.Quantity,
                    ProductType = (ProductType)x.TypeId
                }));
            }
        }

        private async Task AddAllFromOrderAsync()
        {
            OrderDto order = await ErrorHandler.HandleErrorsAsync(
                ct => WebClient.ExecuteApiRequestAsync(new QueryOrder(OrderId.Value)),
                "получении заказа",
                null,
                this,
                true);

            if (order == null)
            {
                return;
            }

            if (!order.Products.Any())
            {
                MessageFacadeService.ShowNotificationWarning("В заказе нет товаров");
                return;
            }

            PagedResult<ProductAttributesDto> barcodeAttributes = await WebClient.ExecuteApiRequestAsync(new QueryProductsAttributesByIds(order.Products.Select(x => x.Id).ToArray(), false));

            NomenclatureItems.Clear();

            NomenclatureItems.AddRange(order.Products.Select(x => new StoreFiscalRegistrarViewItem
            {
                Id = x.Id,
                NameFullUa = x.NameFullUa,
                Barcode = barcodeAttributes.Data.FirstOrDefault(z => z.ProductId == x.Id)?.Barcodes?.FirstOrDefault(z => z.FiscalRegistrar)?.Barcode,
                Quantity = x.Quantity,
                ProductType = (ProductType)x.ProductTypeId,
                Price = x.PriceOut
            }));

            SetToPayAmount();
        }

        private void Remove(StoreFiscalRegistrarViewItem item)
        {
            if (item != null)
            {
                NomenclatureItems.Remove(item);
                SetToPayAmount();
            }
        }

        private void CashboxOperations()
        {
            DialogDocumentManagerService.ShowView<FiscalRegistrarCashboxViewModel>(null, this);
        }

        private void HandleProductPropertyChanged()
        {
            SetToPayAmount();
        }

        private void SetToPayAmount()
        {
            ToPayAmount = NomenclatureItems.Select(x => x.Quantity * x.Price).DefaultIfEmpty(0).Sum();
        }

        private async Task RefreshPaymentTypesAsync()
        {
            List<FiscalDocumentPaymentTypeDto> paymentTypes = await WebClient.ExecuteApiRequestAsync(new QueryFiscalDocumentPaymentTypes());

            PaymentTypes = paymentTypes
                .Select(x => new ComboBoxItem(x.Id, x.Name))
                .ToReadOnlyObservableCollection();

            PaymentTypeId = FiscalPaymentType.CashPayment.Id;
            RaisePropertyChanged(nameof(PaymentTypeId));
        }

        private async Task PrintChequeAsync(ChequeType type)
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

            CashboxDto cashbox = await WebClient.ExecuteApiRequestAsync(new QueryCashbox(clientResult.Data.Settings.CashboxId));

            if (cashbox.Session?.Closed != false)
            {
                MessageFacadeService.ShowNotificationWarning("Смена закрыта");
                return;
            }

            OrderDto order = await WebClient.ExecuteApiRequestAsync(new QueryOrder(OrderId.Value));

            if (order.LegalEntity == null)
            {
                MessageFacadeService.ShowNotificationWarning("В заказе не указано юр. лицо");
                return;
            }

            if (order.LegalEntity.White == false)
            {
                MessageFacadeService.ShowNotificationWarning("Юр. лицо указанное в заказе не фискализируется");
                return;
            }

            string messagePart = string.Empty;

            switch (type)
            {
                case ChequeType.Receive:
                    messagePart = "продажный";
                    break;
                case ChequeType.Refund:
                    messagePart = "возвратный";
                    break;
            }

            DelayedConfirmViewModel viewModel = DialogDocumentManagerService.ShowView<DelayedConfirmViewModel>($"Вы уверены что хотите напечатать {messagePart} чек?", this);

            if (!viewModel.IsOk)
            {
                return;
            }

            if (await CheckOrderAsync(OrderId!.Value) != true)
            {
                return;
            }

            SellProductRequest[] printChequeProductItems = NomenclatureItems
                .GroupBy(x => new { x.Id, x.NameFullUa, x.Barcode, x.Price, x.ProductType })
                .Select(g => new SellProductRequest(g.Key.Id, g.Key.NameFullUa, g.Key.Barcode, g.Key.Price, g.Sum(y => y.Quantity), g.Key.ProductType))
                .ToArray();

            FiscalDocumentCreateDto fiscalDocumentCreateDto = null;

            await ErrorHandler.HandleErrorsAsync(
                async ct =>
                {
                    if (type == ChequeType.Receive)
                    {
                        SellRequest request = new SellRequest(
                            Guid.NewGuid().ToString(),
                            cashbox.Id,
                            $"Касир: {order.LegalEntity.Name}",
                            $"Замовлення №{OrderId}",
                            OrderId.Value,
                            printChequeProductItems,
                            new[] { new SellPaymentRequest(Telemart.Common.Dictionaries.Base.DictionaryItemBase.GetById<FiscalPaymentType>(PaymentTypeId), Amount) },
                            Array.Empty<string>(),
                            null);

                        Result<SellResponse> sellResult = await clientResult.Data.SellAsync(request, ct);

                        fiscalDocumentCreateDto = request.ToDocument(cashbox.Id, sellResult.Data.Id, OrderId.Value, Entity.OrderId, false);

                        return sellResult;
                    }
                    else
                    {
                        ReturnRequest request = new ReturnRequest(
                            Guid.NewGuid().ToString(),
                            cashbox.Id,
                            $"Касир: {order.LegalEntity.Name}",
                            $"Замовлення №{OrderId}",
                            OrderId.Value,
                            printChequeProductItems,
                            new[] { new SellPaymentRequest(Telemart.Common.Dictionaries.Base.DictionaryItemBase.GetById<FiscalPaymentType>(PaymentTypeId), Amount) },
                            Array.Empty<string>());

                        Result<SellResponse> returnResult = await clientResult.Data.ReturnAsync(request, ct);

                        fiscalDocumentCreateDto = request.ToDocument(cashbox.Id, returnResult.Data.Id, OrderId.Value, Entity.OrderId, false);

                        return returnResult;
                    }
                },
                "проведении оплаты через РРО",
                "Оплата в РРО проведена",
                this,
                true,
                onSuccess: async (sellResponse, ct) =>
                {
                    await WebClient.ExecuteApiRequestAsync(new CreateFiscalDocument(fiscalDocumentCreateDto));

                    await SendFiscalToCustomerAsync(clientResult.Data, sellResponse.Data.Id, cashbox.Id, order.Phone, order.Email, ct);

                    IsOk = true;
                    Close();
                });
        }

        private async Task SendFiscalToCustomerAsync(IFiscalRegistrarClient fiscalClient, string fiscalId, int cashboxId, string phone, string email, CancellationToken cancellationToken)
        {
            if (!string.IsNullOrEmpty(phone))
            {
                PhoneNumber phoneNum = new PhoneNumber(phone);

                await ErrorHandler.HandleErrorsAsync(
                    ct => fiscalClient.SendByPhoneAsync(fiscalId, phoneNum.InternationalNumberWithoutPlus, cashboxId, ct),
                    "отправке чека по sms",
                    "Чек по sms отправлен",
                    null,
                    true,
                    false,
                    onSuccess: (_, _) =>
                    {
                        Logger.LogInformation($"Чек {fiscalId} по sms на номер {phoneNum.InternationalNumberWithoutPlus} отправлен");
                        return Task.CompletedTask;
                    },
                    cancellationToken: cancellationToken);
            }

            if (!string.IsNullOrEmpty(email))
            {
                await ErrorHandler.HandleErrorsAsync(
                    ct => fiscalClient.SendToEmailsAsync(fiscalId, new[] { email }, cashboxId, ct),
                    "отправке чека на почту",
                    "Чек на почту отправлен",
                    null,
                    true,
                    false,
                    onSuccess: (_, _) =>
                    {
                        Logger.LogInformation($"Чек {fiscalId} на почту {email} отправлен");
                        return Task.CompletedTask;
                    },
                    cancellationToken: cancellationToken);
            }
        }

        private async Task<bool> CheckOrderAsync(int orderId)
        {
            OrderDto order = default;

            try
            {
                 order = await WebClient.ExecuteApiRequestAsync(new QueryOrder(orderId));

                 if (!Dictionaries.GetItemById<Payment>(order.PaymentId).Fiscal)
                 {
                     MessageFacadeService.ShowMessageBoxError("Способ оплаты заказа не поддерживает фискализацию");

                     return false;
                 }
            }
            catch (UnexpectedSatusException exception) when (exception.Message == "An unexpected status code was returned. (NotFound Resource not found)")
            {
                MessageFacadeService.ShowMessageBoxError("Указанный заказ не существует");

                return false;
            }

            return true;
        }
    }
}