using DevExpress.Mvvm.DataAnnotations;
using DevExpress.Mvvm.POCO;

namespace Telemart.Client.ViewModels.Directories.Category
{
    [POCOViewModel]
    public class CategoryProductCatalogViewItem
    {
        protected CategoryProductCatalogViewItem()
        {
        }

        public virtual int Id { get; set; }

        public virtual int Left { get; set; }

        public virtual int Right { get; set; }

        public virtual string Manufactor { get; set; }

        public virtual string PrefixRus { get; set; }

        public virtual string PrefixUkr { get; set; }

        public virtual string PrefixEn { get; set; }

        public virtual string Name { get; set; }

        public virtual string PartNumber { get; set; }

        public virtual string Keywords { get; set; }

        public virtual int WarrantyRetailId { get; set; }

        public virtual int WarrantyWholesaleId { get; set; }

        public virtual int? WarrantyTypeId { get; set; }

        public virtual int ParentId { get; set; }

        public virtual bool KeepPn { get; set; }

        public virtual double Active { get; set; }

        public virtual int ParentLevel { get; set; }

        public static CategoryProductCatalogViewItem Create()
        {
            return ViewModelSource<CategoryProductCatalogViewItem>.Create();
        }

        public override string ToString()
        {
            return Name;
        }
    }
}