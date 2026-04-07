using DevExpress.Mvvm.DataAnnotations;
using DevExpress.Mvvm.POCO;
using Telemart.Client.Dictionaries;
using Telemart.Client.ViewModels.Directories.Category;

namespace Telemart.Client.ViewModels.Directories.Contractor
{
    [POCOViewModel]
    public class SupplierCategoryAbcViewItem
    {
        protected SupplierCategoryAbcViewItem()
        {
        }

        public virtual int Id { get; set; }

        public virtual int ContractorId { get; set; }

        public virtual int CategoryId { get; set; }

        public virtual CategoryViewItem Category { get; set; }

        public virtual string CategoryDisplayName => string.IsNullOrEmpty(Category?.NameFull) ? Category?.Name : Category?.NameFull;

        public virtual int AbcId { get; set; }

        public virtual AbcType AbcType { get; set; }

        public static SupplierCategoryAbcViewItem Create()
        {
            return ViewModelSource<SupplierCategoryAbcViewItem>.Create();
        }

        protected void OnAbcTypeChanged(AbcType oldValue)
        {
            if (AbcType != null)
            {
                AbcId = AbcType.Id;
            }
        }
    }
}
