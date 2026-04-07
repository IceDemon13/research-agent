using System;

namespace Telemart.Client.Reports.AdditionalServiceProduct
{
    public sealed class AdditionalServiceProductClientProductsReportData
    {
        private string _number;
        private bool _income;

        public AdditionalServiceProductClientProductsReportData(DateTime date, string fullNameClient, string phoneNumber, string place, string employeeName, bool income = false)
        {
            Date = date.ToString("dd.MM.yyyy");
            FullNameClient = fullNameClient;
            PhoneNumber = phoneNumber;
            Place = place;
            EmployeeName = employeeName;
            _income = income;
        }

        public string Number
        {
            get { return $"{_number}.{(_income ? 1 : 2)}"; }
            set { _number = value; }
        }

        public string Date { get; private set; }

        public string FullNameClient { get; private set; }

        public string PhoneNumber { get; private set; }

        public string Place { get; private set; }

        public int Count { get; private set; }

        public string EmployeeName { get; private set; }

        public GuestProductReportData[] GuestProducts { get; private set; }

        public void SetGuestProducs(GuestProductReportData[] guestProductReportDatas)
        {
            GuestProducts = guestProductReportDatas;
            Count = guestProductReportDatas?.Length ?? 0;
        }

        public void SetNumber(int number)
        {
            Number = number.ToString();
        }
    }
}