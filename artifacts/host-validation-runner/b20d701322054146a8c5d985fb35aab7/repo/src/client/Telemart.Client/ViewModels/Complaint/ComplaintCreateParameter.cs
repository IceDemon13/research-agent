using System.Collections.Generic;
using Telemart.Client.Common.MvvmEnhancements;

namespace Telemart.Client.ViewModels.Complaint
{
    public class ComplaintCreateParameter
    {
        public ComplaintCreateParameter(int? orderId, int? serviceRequestId, int? tradeInId, int? contractorId, IReadOnlyCollection<ComboBoxItem> products, string fio, string phone, string phone2, string email)
        {
            OrderId = orderId;
            ServiceRequestId = serviceRequestId;
            TradeInId = tradeInId;
            ContractorId = contractorId;
            Products = products;
            Fio = fio;
            Phone = phone;
            Phone2 = phone2;
            Email = email;
        }

        private ComplaintCreateParameter()
        {
        }

        public static ComplaintCreateParameter Empty { get; } = new ComplaintCreateParameter();

        public int? OrderId { get; }

        public int? ServiceRequestId { get; }

        public int? TradeInId { get; }

        public int? ContractorId { get; }

        public IReadOnlyCollection<ComboBoxItem> Products { get; }

        public string Fio { get; }

        public string Phone { get; }

        public string Phone2 { get; }

        public string Email { get; }
    }
}
