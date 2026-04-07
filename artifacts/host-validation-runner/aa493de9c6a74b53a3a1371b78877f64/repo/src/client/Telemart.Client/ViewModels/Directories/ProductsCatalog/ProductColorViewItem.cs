using DevExpress.Mvvm.DataAnnotations;
using DevExpress.Mvvm.POCO;

namespace Telemart.Client.ViewModels.Directories.ProductsCatalog
{
    [POCOViewModel]
    public class ProductColorViewItem
    {
        protected ProductColorViewItem()
        {
        }

        public virtual int Id { get; set; }

        public virtual string Name { get; set; }

        public virtual string Color { get; set; }

        public static ProductColorViewItem Create()
        {
            return ViewModelSource<ProductColorViewItem>.Create();
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
