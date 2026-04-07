using System.Collections.Generic;
using System.Linq;

namespace Telemart.Client.PosTerminal.Ingenico
{
    public class ReturnRefundResult
    {
        public ReturnRefundResult(string rrn, uint checkNumber, string terminalId, string merchantId, string authCode, string pan, string issuerName)
        {
            Rrn = rrn;
            CheckNumber = checkNumber;
            TerminalId = terminalId;
            MerchantId = merchantId;
            AuthCode = authCode;
            Pan = pan;
            IssuerName = issuerName;
        }

        public string Rrn { get; private set; }

        public uint CheckNumber { get; private set; }

        public string TerminalId { get; private set; }

        public string MerchantId { get; private set; }

        public string AuthCode { get; private set; }

        public string Pan { get; private set; }

        public string  IssuerName { get; private set; }
    }
}