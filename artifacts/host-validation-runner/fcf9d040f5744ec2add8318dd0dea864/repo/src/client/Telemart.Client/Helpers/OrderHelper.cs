using Telemart.Client.Common;
using Telemart.Client.Dictionaries;

namespace Telemart.Client.Helpers
{
    public static class OrderHelper
    {
        public static SummaryViewItem GetPayedInfoSummaryItem(int pko, int? paymentId, string title)
        {
            int level;
            string payedInfo;

            if (pko == 1)
            {
                payedInfo = "Получена";
                level = SummaryViewItem.GreenLevel;
            }
            else
            {
                payedInfo = "Не оплачено";

                if (paymentId == null || paymentId == Payment.CashId || paymentId == Payment.NoId)
                {
                    level = SummaryViewItem.NormalLevel;
                }
                else
                {
                    level = SummaryViewItem.RedLevel;
                }
            }

            return new SummaryViewItem(title, payedInfo, level);
        }
    }
}