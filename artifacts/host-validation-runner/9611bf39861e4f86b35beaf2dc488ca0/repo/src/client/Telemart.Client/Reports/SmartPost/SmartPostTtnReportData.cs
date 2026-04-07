using System;
using Telemart.Client.Business;

namespace Telemart.Client.Reports.SmartPost
{
    public class SmartPostTtnReportData
    {
        public SmartPostTtnReportData(
            int orderId,
            string senderName,
            DateTime dateX,
            decimal price,
            string address,
            string house,
            string flatNumber,
            string recipientName,
            string recipientPhone,
            int placeNumber,
            int totalPlaceNumbers)
        {
            OrderId = orderId;
            TotalPlaceNumbers = totalPlaceNumbers;
            PlaceNumber = placeNumber;
            SenderName = senderName;
            DateX = dateX;
            PriceStr = price.ToString(CurrencyFormatingRules.UahFormat, CurrencyFormatingRules.UahCultureInfo);
            Address = address;
            House = house;
            FlatNumber = flatNumber;
            RecipientName = recipientName;
            RecipientPhone = recipientPhone;
            Barcode = placeNumber == 1
                ? OrderId.ToString()
                : $"{OrderId}/{PlaceNumber}";
        }

        public int OrderId { get; }

        public int TotalPlaceNumbers { get; }

        public int PlaceNumber { get; }

        public string SenderName { get; }

        public DateTime DateX { get; }

        public string PriceStr { get; }

        public string Address { get; }

        public string House { get; }

        public string FlatNumber { get; }

        public string RecipientName { get; }

        public string RecipientPhone { get; }

        public string Barcode { get; }
    }
}