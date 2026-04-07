using System.Collections.ObjectModel;
using DevExpress.Mvvm.DataAnnotations;
using DevExpress.Mvvm.POCO;
using Telemart.Client.Properties;
using Telemart.Client.ViewModels.Directories.Category;

namespace Telemart.Client.ViewModels.Common.Product
{
    [POCOViewModel(ImplementIDataErrorInfo = true)]
    public class ProductCardViewItem
    {
        protected ProductCardViewItem()
        {
        }

        public virtual int ProductId { get; set; }

        public virtual int CategoryId { get; set; }

        public virtual int? TypeId { get; set; }

        public virtual CategoryViewItem Category { get; set; }

        public virtual int? ProductDayCategoryId { get; set; }

        public virtual int? ProductDayPosition { get; set; }

        public virtual string Name { get; set; }

        public virtual string NameFullUa { get; set; }

        public virtual bool KeepSerial { get; set; }

        public virtual bool SelfBarcode { get; set; }

        public virtual double? Width { get; set; }

        public virtual double? Height { get; set; }

        public virtual double? Depth { get; set; }

        public virtual decimal? Weight { get; set; }

        public virtual double Active { get; set; }

        public virtual int CurrencyId { get; set; }

        public virtual ObservableCollection<ProductBarcodeViewItem> Barcodes { get; set; }

        public virtual ObservableCollection<ProductSnLengthViewItem> SerialNumberLength { get; set; }

        public static void BuildMetadata(MetadataBuilder<ProductCardViewItem> builder)
        {
            builder.Property(x => x.ProductDayPosition)
                .MatchesInstanceRule((x, y) => y.ProductDayCategoryId == null || x.HasValue, () => Resources.RequiredErrorMessage);
            builder.Property(x => x.TypeId).Required(() => Resources.RequiredErrorMessage);
        }

        public static ProductCardViewItem Create()
        {
            return ViewModelSource<ProductCardViewItem>.Create();
        }

        protected void OnProductDayCategoryIdChanged(int? oldValue)
        {
            this.RaisePropertyChanged(x => x.ProductDayPosition);
        }
    }
}
