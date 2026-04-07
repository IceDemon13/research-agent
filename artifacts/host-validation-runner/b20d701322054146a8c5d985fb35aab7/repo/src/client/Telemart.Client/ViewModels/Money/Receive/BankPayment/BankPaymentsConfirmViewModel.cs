using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using DevExpress.Xpf.Grid;
using Telemart.Client.Business;
using Telemart.Client.Common.Messages;
using Telemart.Client.Common.Services;
using Telemart.Client.Core.Helpers;
using Telemart.Client.Data.Requests.Features.Order;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Data.WebClient.Security;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.TransferObjects;
using Telemart.Client.ViewModels.Base;
using Telemart.Client.ViewModels.Store;
using Telemart.Client.ViewModels.Validation;

namespace Telemart.Client.ViewModels.Money.Receive.BankPayment
{
    public class BankPaymentsConfirmViewModel : TelemartDialogViewModelBase
    {
        public BankPaymentsConfirmViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            IMapper mapper,
            IMessenger messenger)
            : base(webClient, dictionaries, messageFacadeService)
        {
            Mapper = mapper;
            Messenger = messenger;

            HandleRowDoubleClickCommand = new DelegateCommand<RowDoubleClickEventArgs>(HandleRowDoubleClick);
        }

        public IDelegateCommand HandleRowDoubleClickCommand { get; }

        public ReadOnlyObservableCollection<BankPaymentConfirmViewItem> Payments
        {
            get { return GetProperty(() => Payments); }
            private set { SetProperty(() => Payments, value); }
        }

        public override int Width { get; } = 750;

        public override int Height { get; } = 420;

        private IMapper Mapper { get; }

        private IMessenger Messenger { get; }

        protected override async Task HandleLoadedAsync()
        {
            IReadOnlyCollection<BankPaymentViewItem> bankPayments = (IReadOnlyCollection<BankPaymentViewItem>)Parameter;

            IEnumerable<int> orderIds = bankPayments.Select(x => x.OrderId.Value);

            IFilteringItem filteringItem = new OrderFilteringItem(string.Empty, new List<int>()) { OrderNumbers = string.Join(", ", orderIds) };

            List<OrderDto> orders = await WebClient.ExecuteApiRequestAsync(new QueryOrders(filteringItem)).GetPagedResultDataAsync();

            IReadOnlyDictionary<int, BankPaymentConfirmOrderViewItem> orderDictionary = orders.ToDictionary(x => x.Id, y => Mapper.Map<BankPaymentConfirmOrderViewItem>(y));

            Payments = bankPayments.Select(x => new BankPaymentConfirmViewItem
            {
                Id = x.Id,
                OrderId = x.OrderId,
                ParsedOrderId = x.ParsedOrderId,
                Comment = x.Comment,
                CurrencyId = x.CurrencyId,
                Amount = x.Amount,
                Order = orderDictionary.GetValueOrDefault(x.OrderId.Value)
            }).ToReadOnlyObservableCollection();

            Title = "Подтверждение платежей";
        }

        protected override Task HandleOkAsync()
        {
            List<ValidationResultItem> validationResultItems = new List<ValidationResultItem>();

            foreach (BankPaymentConfirmViewItem payment in Payments)
            {
                if (payment.Order == null)
                {
                    validationResultItems.Add(new ValidationResultItem($"Заказ №{payment.OrderId} не найден", true));
                }
                else
                {
                    OrderStatus[] states = GetValidStates(payment.Order).ToArray();

                    if (!WebClient.IsOperationAllowed(BusinessOperation.OrderPayWithdrawIgnoreChecks)
                        && !states.Contains(payment.Order.State))
                    {
                        validationResultItems.Add(new ValidationResultItem(
                            $"Заказ №{payment.OrderId} должен быть в статусе: {string.Join(", ", states.Select(x => x.Name))}", true));
                    }
                }
            }

            if (validationResultItems.Any())
            {
                ShowValidationResultView("Ошибки", validationResultItems);
            }
            else
            {
                if (MessageFacadeService.Confirm($"Вы уверены, что хотите провести {Payments.Count} {NumToPayments(Payments.Count)} на сумму {GetTotalPaymentsAmount()}?"))
                {
                    IsOk = true;
                    Close();
                }
            }

            return Task.CompletedTask;

            string GetTotalPaymentsAmount()
            {
                if (Payments?.Any() != true)
                {
                    return string.Empty;
                }

                Prices prices = new Prices(0, 0, 0);

                foreach (Price price in Payments.Select(x => new Price(x.Amount, x.CurrencyId)))
                {
                    prices += price;
                }

                return CurrencyFormatingRules.ToPricesString(prices, "C2");
            }

            string NumToPayments(int num)
            {
                return WordEndingHelper.GetWordByNumber(num, "платеж", "платежа", "платежей");
            }

            IEnumerable<OrderStatus> GetValidStates(BankPaymentConfirmOrderViewItem order)
            {
                yield return OrderStatus.Received;
                if (order?.ExternalPayments?.Any(x => x.ReceivedAmount > 0) == true)
                {
                    yield return OrderStatus.Confirmed;
                    yield return OrderStatus.Packed;
                    yield return OrderStatus.Done;
                    yield return OrderStatus.Returned;
                }
            }
        }

        private void HandleRowDoubleClick(RowDoubleClickEventArgs args)
        {
            BankPaymentConfirmViewItem item = (BankPaymentConfirmViewItem)((GridControl)args.Source.DataControl).CurrentItem;

            switch (args.HitInfo.Column.FieldName)
            {
                case nameof(item.OrderId):
                    if (item.OrderId.HasValue)
                    {
                        Messenger.Send(new OrderEditViewMessage(item.OrderId.Value));
                    }

                    break;
            }
        }
    }
}