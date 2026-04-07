using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Telemart.Client.TransferObjects.PosTerminal;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.PosTerminal.Ingenico
{
    public interface IPosTerminalClient
    {
        PosSettingsDto Settings { get; set; }

        void ReConnect();

        void Connect(string ip, int port);

        Task<Result<TerminalPurchaseResult>> PurchaseAsync(decimal amount, decimal discount, string merchantId, IProgress<string> progress, CancellationToken cancellationToken);

        Task<Result<ReturnRefundResult>> ReturnRefundAsync(
            decimal amount,
            bool fullAmountTransaction,
            string merchantId,
            string rn,
            string rrn,
            uint? invoiceNum,
            decimal? discount,
            DateTime? dateTimePurchase,
            IProgress<string> progress,
            CancellationToken cancellationToken);

        bool IsConnected();
    }
}