using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.PosTerminal.Ingenico
{
    public class IngenicoUkrsibbankPosTerminalClient : IngenicoPosTerminalClient
    {
        public IngenicoUkrsibbankPosTerminalClient(ILogger<IngenicoUkrsibbankPosTerminalClient> logger)
            : base(logger)
        {
        }

        public override async Task<Result<ReturnRefundResult>> ReturnRefundAsync(decimal amount, bool fullAmountTransaction, string merchantId, string rn, string rrn, uint? invoiceNum, decimal? discount, DateTime? dateTimePuchase, IProgress<string> progress, CancellationToken cancellationToken)
        {
            const string GeneralErrorMessage = "Failed to refund terminal payment";
            List<string> errors = new List<string>();

            if (!dateTimePuchase.HasValue)
            {
                return Result<ReturnRefundResult>.Error(GeneralErrorMessage, errors);
            }

            /*if (dateTimePuchase.Value.Date == DateTime.Now.Date && !fullAmountTransaction)
            {
                errors.Add($"Частичный возврат недоступен в день проведения транзакции.{Environment.NewLine}Попробуйте завтра или выберите другой способ возврата");

                return errors.ToArray();
            }*/

            if (dateTimePuchase.Value.Date == DateTime.Now.Date && fullAmountTransaction)
            {
                if (!invoiceNum.HasValue)
                {
                    errors.Add("У данной транзакции не заполнен номер чека");

                    return Result<ReturnRefundResult>.Error(GeneralErrorMessage, errors);
                }

                return await CancelAsync(invoiceNum.Value, merchantId, progress, cancellationToken);
            }

            if (string.IsNullOrEmpty(rrn))
            {
                errors.Add("У данной транзакции не заполнен RRN");

                return Result<ReturnRefundResult>.Error(GeneralErrorMessage, errors);
            }

            return await RefundAsync(amount, discount ?? 0, merchantId, rrn, progress, cancellationToken);
        }
    }
}