namespace Telemart.Client.PosTerminal.Ingenico
{
    public class TerminalPurchaseResult
    {
        public TerminalPurchaseResult(string rrn, uint checkNumber, string terminalId, string merchantId, string authCode, string pan, string issuerName)
        {
            Rrn = rrn;
            CheckNumber = checkNumber;
            TerminalId = terminalId;
            MerchantId = merchantId;
            AuthCode = authCode;
            Pan = pan;
            IssuerName = issuerName;
        }

        public string Rrn { get; }

        public uint CheckNumber { get; }

        public string TerminalId { get; }

        public string MerchantId { get; }

        public string AuthCode { get; }

        public string Pan { get; }

        public string  IssuerName { get; }

        public string Rn { get; private set; }

        public void SetRn(string rn)
        {
            Rn = rn;
        }
    }
}