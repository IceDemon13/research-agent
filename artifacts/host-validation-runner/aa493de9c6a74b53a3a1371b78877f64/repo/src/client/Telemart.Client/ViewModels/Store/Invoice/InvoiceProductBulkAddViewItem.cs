using System;
using DevExpress.Mvvm;
using Telemart.Client.Dictionaries;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.ViewModels.Store.Invoice
{
    public class InvoiceProductBulkAddViewItem : BindableBase
    {
        public InvoiceProductBulkAddViewItem(int? productId, string productName, string supplierProductId, string supplierProductName, string pn, int quantity, decimal price)
        {
            ProductId = productId;
            SupplierProductId = supplierProductId;
            Pn = pn;
            Quantity = quantity;
            Price = price;
            SupplierProductName = supplierProductName;
            ProductName = productName;
        }

        public InvoiceProductBulkAddViewItem()
        {
        }

        public int? ProductId
        {
            get { return GetProperty(() => ProductId); }
            set { SetProperty(() => ProductId, value); }
        }

        public string ProductName
        {
            get { return GetProperty(() => ProductName); }
            set { SetProperty(() => ProductName, value); }
        }

        public string SupplierProductId
        {
            get { return GetProperty(() => SupplierProductId); }
            set { SetProperty(() => SupplierProductId, value); }
        }

        public string SupplierProductName
        {
            get { return GetProperty(() => SupplierProductName); }
            set { SetProperty(() => SupplierProductName, value); }
        }

        public string Pn
        {
            get { return GetProperty(() => Pn); }
            set { SetProperty(() => Pn, value); }
        }

        public bool? PriceWithTax
        {
            get { return GetProperty(() => PriceWithTax); }
            set { SetProperty(() => PriceWithTax, value); }
        }

        public decimal? Price
        {
            get { return GetProperty(() => Price); }
            set { SetProperty(() => Price, value); }
        }

        public ProductDto Product
        {
            get { return GetProperty(() => Product); }
            set { SetProperty(() => Product, value); }
        }

        public int Quantity
        {
            get { return GetProperty(() => Quantity); }
            set { SetProperty(() => Quantity, value); }
        }

        public TaxRate TaxRate
        {
            get { return GetProperty(() => TaxRate); }
            set { SetProperty(() => TaxRate, value); }
        }

        public bool IsValid => Product != null;

        public decimal? GetPriceWithTaxCalculation()
        {
            if (Price is null)
            {
                return null;
            }

            if (PriceWithTax == false)
            {
                return Math.Round(Price.Value * (100m + TaxRate.Value) / 100m, 2);
            }

            return Price;
        }
    }
}