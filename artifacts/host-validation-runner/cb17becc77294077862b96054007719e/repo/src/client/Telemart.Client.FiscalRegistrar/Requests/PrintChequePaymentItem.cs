namespace Telemart.Client.FiscalRegistrar.Requests
{
    public class PrintChequePaymentItem
    {
        public PrintChequePaymentItem(int no, decimal sum)
        {
            No = no;
            Sum = sum;
        }

        public int No { get; }

        public decimal Sum { get; }
    }
}
