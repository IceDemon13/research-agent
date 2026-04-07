using System;
using DevExpress.Mvvm.DataAnnotations;
using DevExpress.Mvvm.POCO;
using Telemart.Client.Dictionaries;

namespace Telemart.Client.ViewModels.Store.Invoice
{
    [POCOViewModel(ImplementIDataErrorInfo = true)]
    public class InvoiceTemplateViewItem
    {
        protected InvoiceTemplateViewItem()
        {
        }

        public virtual CarryType CarryType { get; set; }

        public virtual int? SupplierWarehouseId { get; set; }

        public virtual string SupplierWarehouseName { get; set; }

        public virtual int PaymentId { get; set; }

        public virtual DateTime DateClose { get; set; }

        public virtual DateTime DateGet { get; set; }

        public virtual DateTime DateArrive { get; set; }

        public virtual InvoiceState State { get; set; }

        public virtual int SupplierId { get; set; }

        public virtual int WarehouseId { get; set; }

        public bool? Main { get; set; }

        public virtual string CarryName => $"{CarryType.Name} {SupplierWarehouseName}";

        public static void BuildMetadata(MetadataBuilder<InvoiceTemplateViewItem> builder)
        {
            builder.Property(x => x.CarryType).Required(() => "Доставка должна быть заполнена");
            builder.Property(x => x.SupplierWarehouseId).Required(() => "Поставщик должен быть заполнен");
        }

        public static InvoiceTemplateViewItem Create()
        {
            return ViewModelSource<InvoiceTemplateViewItem>.Create();
        }

        protected void OnCarryTypeChanged(CarryType oldCarryType)
        {
            this.RaisePropertyChanged(x => x.CarryName);
        }

        protected void OnSupplierWarehouseNameChanged(string oldSupplierWarehouseName)
        {
            this.RaisePropertyChanged(x => x.CarryName);
        }
    }
}
