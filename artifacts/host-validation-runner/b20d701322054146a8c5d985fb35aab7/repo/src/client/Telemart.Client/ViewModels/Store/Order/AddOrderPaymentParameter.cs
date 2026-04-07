using System;
using Telemart.Client.Business;
using Telemart.Client.Dictionaries;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.ViewModels.Store.Order
{
    public sealed class AddOrderPaymentParameter
    {
        public AddOrderPaymentParameter(DateTime minReceivedOn, LegalEntityDto legalEntity, Prices toPayAmount, Currency[] currencies, int clientId, bool fullAmount, int paymentId)
        {
            MinReceivedOn = minReceivedOn;
            LegalEntity = legalEntity;
            ToPayAmount = toPayAmount;
            Currencies = currencies;
            ClientId = clientId;
            FullAmount = fullAmount;
            PaymentId = paymentId;
        }

        public DateTime MinReceivedOn { get; }

        public LegalEntityDto LegalEntity { get; }

        public Prices ToPayAmount { get; }

        public Currency[] Currencies { get; }

        public int ClientId { get; }

        public bool FullAmount { get; }

        public int PaymentId { get; }
    }
}