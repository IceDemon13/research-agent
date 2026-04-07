using System.Collections.Generic;
using Telemart.Client.TransferObjects;
using Telemart.Client.ViewModels.Store.Order.OrderPaymentInfo;

namespace Telemart.Client.ViewModels.Store.Order
{
    public sealed class AddOrderWithdrawParameter
    {
        public AddOrderWithdrawParameter(
            int orderId,
            IReadOnlyCollection<IOrderPayment> orderPayments,
            LegalEntityDto legalEntity,
            int? paymentTypeId = null,
            int? currencyId = null,
            int? cashboxId = null,
            decimal amount = 0,
            bool readOnly = false,
            string firstName = null,
            string lastName = null,
            string middleName = null)
        {
            OrderId = orderId;
            LegalEntity = legalEntity;
            PaymentTypeId = paymentTypeId;
            CurrencyId = currencyId;
            CashboxId = cashboxId;
            ReadOnly = readOnly;
            Amount = amount;
            OrderPayments = orderPayments;
            FirstName = firstName;
            LastName = lastName;
            MiddleName = middleName;
        }

        public int OrderId { get; }

        public LegalEntityDto LegalEntity { get; }

        public int? PaymentTypeId { get; }

        public int? CurrencyId { get; }

        public int? CashboxId { get; }

        public bool ReadOnly { get; }

        public decimal Amount { get; }

        public string FirstName { get; }

        public string LastName { get; }

        public string MiddleName { get; }

        public IReadOnlyCollection<IOrderPayment> OrderPayments { get; }
    }
}