using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using DevExpress.Xpf.Editors;
using Telemart.Client.Common;
using Telemart.Client.Common.Services;
using Telemart.Client.Data.Requests.Features.Order;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.TransferObjects;
using Telemart.Client.ViewModels.Base;
using Telemart.Client.Views.Store.Order;

namespace Telemart.Client.ViewModels.Store.Order
{
    [ViewName(nameof(OrderEditPaymentView))]
    public abstract class OrderEditPaymentViewModelBase : TelemartDialogViewModelBase
    {
        protected OrderEditPaymentViewModelBase(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService)
            : base(webClient, dictionaries, messageFacadeService)
        {
            ValidatePaymentCommand = new DelegateCommand<ValidationEventArgs>(ValidatePayment);
        }

        public IDelegateCommand ValidatePaymentCommand { get; }

        #region INPC

        public ObservableCollection<Payment> AllPayments
        {
            get { return GetProperty(() => AllPayments); }
            private set { SetProperty(() => AllPayments, value); }
        }

        public ObservableCollection<Payment> AllowedPayments
        {
            get { return GetProperty(() => AllowedPayments); }
            private set { SetProperty(() => AllowedPayments, value); }
        }

        public int CurrentPaymentId
        {
            get { return GetProperty(() => CurrentPaymentId); }
            set { SetProperty(() => CurrentPaymentId, value); }
        }

        public int SelectedPaymentId
        {
            get { return GetProperty(() => SelectedPaymentId); }
            set { SetProperty(() => SelectedPaymentId, value); }
        }

        public decimal OrderTotalAmount
        {
            get { return GetProperty(() => OrderTotalAmount); }
            set { SetProperty(() => OrderTotalAmount, value); }
        }

        public bool ReadOnlySelectedPayment
        {
            get { return GetProperty(() => ReadOnlySelectedPayment); }
            set { SetProperty(() => ReadOnlySelectedPayment, value); }
        }

        #endregion

        protected override async Task HandleLoadedAsync()
        {
            OrderEditPaymentParameter parameter = (OrderEditPaymentParameter)Parameter;

            AllPayments = Dictionaries.GetItems<Payment>().ToObservableCollection();

            if (parameter.ChangePayment)
            {
                OrderDto orderDto = await WebClient.ExecuteApiRequestAsync(new QueryOrder(parameter.OrderId));

                AllowedPayments = Dictionaries.GetItems<Payment>()
                    .Where(x => Dictionaries.GetPaymentsBySubdivision(parameter.SubdivisionId).Contains(x.Id) &&
                                x.Active && !x.OnlyCreateOnWeb && x.Id != orderDto.PaymentId)
                    .ToObservableCollection();
            }
            else
            {
                AllowedPayments = Dictionaries.GetItems<Payment>()
                    .Where(x => Dictionaries.GetPaymentsBySubdivision(parameter.SubdivisionId).Contains(x.Id) && x.Active && !x.OnlyCreateOnWeb)
                    .ToObservableCollection();
            }

            ReadOnlySelectedPayment = parameter.SelectedPaymentId.HasValue;
            CurrentPaymentId = parameter.PaymentId;
            SelectedPaymentId = parameter.SelectedPaymentId ?? parameter.PaymentId;
            OrderTotalAmount = parameter.OrderTotalAmount;

            Title = "Изменение способа оплаты";
        }

        private void ValidatePayment(ValidationEventArgs args)
        {
            if (args.Value is int selectedPaymentId and Payment.CashId && OrderTotalAmount >= Constants.MaxOrderCashAmount)
            {
                args.IsValid = false;
                args.ErrorType = DevExpress.XtraEditors.DXErrorProvider.ErrorType.Warning;
                args.ErrorContent = "Способ оплаты 'Наличные' не доступен для заказов на сумму более 49,999 грн";
                args.Handled = true;
                SelectedPaymentId = selectedPaymentId;
            }
        }
    }
}