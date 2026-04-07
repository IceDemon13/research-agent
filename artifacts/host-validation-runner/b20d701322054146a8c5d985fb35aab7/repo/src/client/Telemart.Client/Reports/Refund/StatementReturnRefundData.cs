using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AngleSharp.Common;

namespace Telemart.Client.Reports.Refund
{
    public sealed class StatementReturnRefundData
    {
        public StatementReturnRefundData(string nameCastomer, string phone, string email, DateTime orderDate, string productName, string iban, string inn, string cardNamber)
        {
            NameCustomer = nameCastomer;
            Phone = phone;
            Email = email;
            OrderDate = orderDate.ToString("dd.MM.yyyy");
            ProductName = productName;
            Iban = iban;
            Inn = inn;
            CardNumber = cardNamber;
            DateTimePrint = DateTime.Now.Date.ToString("dd.MM.yyyy");
        }

        public string NameCustomer { get; set; }

        public string Phone { get; set; }

        public string Email { get; set; }

        public string OrderDate { get; set; }

        public string ProductName { get; set; }

        public string Iban { get; set; }

        public string Inn { get; set; }

        public string CardNumber { get; set; }

        public string DateTimePrint { get; set; }
    }
}