using Telemart.Client.FiscalRegistrar.Entities;

namespace Telemart.Client.FiscalRegistrar.Requests
{
    public class PrintIOChequeItem
    {
        public PrintIOChequeItem(IOChequeType type, decimal sum, int num)
        {
            switch (type)
            {
                case IOChequeType.Receive:
                    Sum = sum > 0
                        ? sum
                        : -sum;
                    break;
                case IOChequeType.Refund:
                    Sum = sum < 0
                        ? sum
                        : -sum;
                    break;
            }

            Num = num;
        }

        public decimal Sum { get; }

        public int Num { get; }
    }
}
