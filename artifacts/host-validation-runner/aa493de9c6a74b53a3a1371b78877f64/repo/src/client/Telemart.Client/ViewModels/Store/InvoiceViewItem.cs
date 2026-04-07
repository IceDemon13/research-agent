using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Media;
using DevExpress.Mvvm.DataAnnotations;
using DevExpress.Mvvm.POCO;
using Telemart.Client.Business;
using Telemart.Client.Dictionaries;
using Telemart.Client.TransferObjects;
using Telemart.Client.ViewModels.Store.Invoice;

namespace Telemart.Client.ViewModels.Store
{
    [POCOViewModel]
    public class InvoiceViewItem
    {
        protected InvoiceViewItem()
        {
            AnalyzeWasDone = true;
        }

        public virtual int CarryId { get; set; }

        public virtual CarryType CarryType { get; set; }

        public virtual DateTime DateClose { get; set; }

        public virtual DateTime DateGet { get; set; }

        public virtual DateTime DateArrive { get; set; }

        public virtual bool IgnoreTransit { get; set; }

        public virtual int? ReceivedBy { get; set; }

        public virtual DateTime? ReceivedOn { get; set; }

        public virtual string Comment { get; set; }

        public virtual DateTime? ArrivedOn { get; set; }

        public virtual int? EmployeeLockId { get; set; }

        public virtual int Id { get; set; }

        public virtual ImageSource PriceImage { get; set; }

        public virtual InvoiceState State { get; set; }

        public virtual int SupplierId { get; set; }

        public virtual string CurrencyRateStr { get; set; }

        public virtual string SupplierTelegramChatId { get; set; }

        public virtual bool SupplierCurrencyManual { get; set; }

        public virtual bool SupplierAllowDocuments { get; set; }

        public virtual int? SupplierWarehouseId { get; set; }

        public virtual string SupplierWarehouseName { get; set; }

        public virtual bool? SupplierAutoReserve { get; set; }

        public virtual bool? SupplierAutoPurchase { get; set; }

        public virtual bool? SourceCurrentDateX { get; set; }

        public virtual Prices TotalPrice { get; set; }

        public virtual int WarehouseId { get; set; }

        public virtual string WarehouseName { get; set; }

        public virtual string WarehouseCityName { get; set; }

        public virtual string WarehouseAddress { get; set; }

        public bool AnalyzeWasDone { get; set; }

        public double? ProductsWeight => InvoiceProducts?.Sum(x => x.Weight * x.Quantity);

        public int? ProductsBillsQuantity => InvoiceProducts?.Sum(x => x.BillsQuantity);

        public int? ProductsQuantityExceptReturned => InvoiceProducts?.Sum(x => x.QuantityReal - x.QuantityReturned);

        public bool ProductQuantityVisible => State == InvoiceState.Received && SupplierAllowDocuments;

        public string ProductQuantityStr => ProductQuantityVisible ? $"{ProductsBillsQuantity}/{ProductsQuantityExceptReturned}" : string.Empty;

        public virtual EmployeeSimpleDto EmployeeLock { get; set; }

        public virtual EmployeeSimpleDto EmployeeCarrier { get; set; }

        public virtual ObservableCollection<InvoiceProductViewItem> InvoiceProducts { get; set; }

        public virtual ObservableCollection<InvoiceAdditionalCostViewItem> AdditionalCosts { get; set; }

        public virtual ObservableCollection<InvoiceCurrencyRateDto> CurrencyRates { get; set; }

        public virtual ObservableCollection<InvoiceTtnViewItem> InvoiceTtns { get; set; }

        public string TtnsStr => string.Join(", ", InvoiceTtns.Select(x => x.Ttn));

        public string TotalQuantityStr => $"{InvoiceProducts?.Count}/{InvoiceProducts?.Sum(x => x.Quantity)}";

        public bool ReservedQuantityVisible => SupplierAutoReserve == true || InvoiceProducts.Any(x => x.QuantityReserved > 0);

        public static InvoiceViewItem Create()
        {
            return ViewModelSource<InvoiceViewItem>.Create();
        }

        public void OnProductPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(InvoiceProductViewItem.Quantity))
            {
                AnalyzeWasDone = false;
            }
        }

        protected void OnInvoiceTtnsChanged()
        {
            this.RaisePropertyChanged(x => x.TtnsStr);
        }

        protected void OnInvoiceProductsChanged()
        {
            AnalyzeWasDone = false;
            this.RaisePropertyChanged(x => x.ProductsWeight);
        }
    }
}