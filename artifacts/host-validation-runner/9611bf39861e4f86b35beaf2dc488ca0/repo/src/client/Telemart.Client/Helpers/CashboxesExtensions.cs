using System.Collections.Generic;
using System.Linq;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries.Constants;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.Cashbox;

namespace Telemart.Client.Helpers
{
    public static class CashboxesExtensions
    {
        public static IEnumerable<CashboxDto> ForIncome(this IEnumerable<CashboxDto> source, IWebClient webClient, int currencyId, int? paymentId)
        {
            return ForReturn(source.Where(x => webClient.AuthenticatedEmployee.AllowCashboxes.Contains(x.Id)), currencyId, paymentId);
        }

        public static IEnumerable<CashboxDto> ForReturn(this IEnumerable<CashboxDto> source, int currencyId, int? paymentId, int? selectedCashboxId = null)
        {
            return source
                .Where(x => x.Id == selectedCashboxId
                            || (x.IsActive && x.CurrencyId == currencyId && x.AllowedPayments.Contains(paymentId ?? CashboxConstants.NotSetCashboxPaymentId)))
                .OrderBy(x => x.Name);
        }

        public static IEnumerable<CashboxDto> ForLegalEntity(this IEnumerable<CashboxDto> source, LegalEntityDto legalEntity, bool anyNotWhite = true, int? selectedCashboxId = null)
        {
            foreach (CashboxDto cashbox in source)
            {
                if (cashbox.Id == selectedCashboxId)
                {
                    yield return cashbox;
                }
                else if (legalEntity == null || cashbox.LegalEntityId == null || cashbox.LegalEntityId == legalEntity.Id)
                {
                    yield return cashbox;
                }
                else if (anyNotWhite && legalEntity.White == false && cashbox.LegalEntity?.White == false)
                {
                    yield return cashbox;
                }
            }
        }
    }
}