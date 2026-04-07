using System;
using System.Globalization;

namespace Telemart.Client.Reports.Common
{
    public class StickerProductReportData
    {
        public StickerProductReportData(
            string productName,
            string featureName,
            DateTime dateCreate,
            string manufacture,
            string manufactureAdress,
            string telemartName,
            string telemartAddress,
            string telemartPhone,
            string telemartEmail)
        {
            ProductName = productName;
            FeatureName = featureName;
            DateCreate = dateCreate.ToString("MM/yyyy", CultureInfo.InvariantCulture);
            ManufactureName = manufacture;
            ManufactureAdress = manufactureAdress;
            TelemartAddress = telemartAddress;
            TelemartPhone = telemartPhone;
            TelemartEmail = telemartEmail;
            TelemartName = telemartName;
        }

        public string ProductName { get; }

        public string FeatureName { get; }

        public string DateCreate { get; }

        public string ManufactureName { get; }

        public string ManufactureAdress { get; }

        public string TelemartAddress { get; }

        public string TelemartName { get; }

        public string TelemartPhone { get; }

        public string TelemartEmail { get; }
    }
}
