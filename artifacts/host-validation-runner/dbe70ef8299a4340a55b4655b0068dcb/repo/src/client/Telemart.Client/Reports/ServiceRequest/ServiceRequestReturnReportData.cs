using System;
using System.Globalization;

namespace Telemart.Client.Reports.ServiceRequest
{
    public class ServiceRequestReturnReportData
    {
        public ServiceRequestReturnReportData(int id, DateTime createdOn, string fio, string phone, string productName, decimal productPrice)
        {
            Id = id;
            CreatedOn = createdOn.ToString("D", new CultureInfo("uk-Ua"));
            Fio = fio;
            Phone = phone;
            ProductName = productName;
            ProductPrice = productPrice;
        }

        public int Id { get; }

        public string CreatedOn { get; }

        public string Fio { get; }

        public string Phone { get; }

        public string ProductName { get; }

        public decimal ProductPrice { get; }
    }
}
