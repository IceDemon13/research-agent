using System.Collections.Generic;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.PosTerminal;
using Telemart.Client.ViewModels.Store.Order.OrderPaymentInfo;

namespace Telemart.Client.ViewModels.Store.Order
{
    public class AddOrderPaymentViaCashRegistrarParameter
    {
        public AddOrderPaymentViaCashRegistrarParameter(IOrderPaymentInfo order, decimal? amount, bool fullAmount, LegalEntityDto orderLegalEntity, bool printCheque, bool prepayment)
        {
            Amount = amount;
            FullAmount = fullAmount;
            OrderLegalEntity = orderLegalEntity;
            PrintCheque = printCheque;
            Prepayment = prepayment;
            Order = order;
        }

        public decimal? Amount { get; }

        public bool FullAmount { get; }

        public IOrderPaymentInfo Order { get; }

        public LegalEntityDto OrderLegalEntity { get; }

        public bool PrintCheque { get; }

        public bool Prepayment { get; }
    }
}