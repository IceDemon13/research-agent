using System;
using System.Globalization;

namespace Telemart.Client.Reports.Refund
{
    public class RefundReportData
    {
        public RefundReportData(string currentEmployeeShortName, decimal price, string legalEntityName, string edrpou, string productName = "Передплата")
        {
            ProductNameStr = productName;
            CurrentEmployeeShortName = currentEmployeeShortName;

            int decPlaces = (int)((price % 1) * 100);

            PriceStr = $"{price:f0} грн. {decPlaces} коп.";

            NowStr = string.Format(CultureInfo.GetCultureInfo("Uk"), "{0:\\\"dd\\\" MMMM yyyy р.}", DateTime.Today);

            LegalEntityName = legalEntityName;

            Edrpou = edrpou;
        }

        public string NowStr { get; }

        public string ProductNameStr { get; }

        public string CurrentEmployeeShortName { get; }

        public string PriceStr { get; }

        public string LegalEntityName { get; }

        public string Edrpou { get; }
    }
}