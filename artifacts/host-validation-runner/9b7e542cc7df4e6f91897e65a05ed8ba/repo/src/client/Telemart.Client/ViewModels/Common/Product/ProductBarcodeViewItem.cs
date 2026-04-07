using System;
using DevExpress.Mvvm.POCO;

namespace Telemart.Client.ViewModels.Common.Product
{
    public class ProductBarcodeViewItem
    {
        protected ProductBarcodeViewItem()
        {
        }

        public virtual int Id { get; set; }

        public virtual int ProductId { get; set; }

        public virtual string Barcode { get; set; }

        public virtual DateTime CreatedOn { get; set; }

        public virtual int CreatedBy { get; set; }

        public virtual DateTime ModifiedOn { get; set; }

        public virtual int ModifiedBy { get; set; }

        public virtual bool Active { get; set; }

        public virtual bool FiscalRegistrar { get; set; }

        public static ProductBarcodeViewItem Create()
        {
            return ViewModelSource<ProductBarcodeViewItem>.Create();
        }
    }
}