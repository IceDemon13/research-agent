using System;
using System.Xml.Serialization;

namespace Telemart.Client.ViewModels.SupplierBill.Requests
{
    [XmlRoot("request")]
    public class BillRequest
    {
        public BillRequest(string edrpou, string number, int invoiceId, int carryId, int currencyCode, DateTime invoicedOn, BillProductRequest[] products)
        {
            Id = edrpou;
            Number = number;
            Date = invoicedOn.ToString("dd.MM.yyyy");
            Products = products;
            InvoiceId = invoiceId;
            CurrencyCode = currencyCode;
            CarryId = carryId;
        }

        public BillRequest()
        {
        }

        [XmlElement("id")]
        public string Id { get; set; }

        [XmlElement("id_invoice")]
        public int InvoiceId { get; set; }

        [XmlElement("id_carry")]
        public int CarryId { get; set; }

        [XmlElement("currency_code")]
        public int CurrencyCode { get; set; }

        [XmlElement("date")]
        public string Date { get; set; }

        [XmlElement("number")]
        public string Number { get; set; }

        [XmlArray("products")]
        [XmlArrayItem("product")]
        public BillProductRequest[] Products { get; set; }
    }
}