using System.Linq;
using DevExpress.Mvvm.DataAnnotations;
using Telemart.Client.Dictionaries;
using Telemart.Client.TransferObjects;
using Telemart.Client.ViewModels.Base;
using Telemart.Client.ViewModels.Nomenclature;

namespace Telemart.Client.ViewModels.Store.Order.Assembly
{
    public class AssemblyViewItem : TelemartViewItemBase
    {
        public AssemblyViewItem(CategoryDto category, bool uniqueComplect, int[] supportedCategoryIds, bool readOnly, bool assemblyIncluded, bool isGift)
        {
            Category = category;
            UniqueComplect = uniqueComplect;
            SupportedCategoryIds = supportedCategoryIds;

            ReadOnly = readOnly;
            AssemblyIncluded = assemblyIncluded;
            IsGift = isGift;
        }

        public CategoryDto Category { get; }

        public bool UniqueComplect { get; }

        public bool ReadOnly
        {
            get { return GetProperty(() => ReadOnly); }
            private set { SetProperty(() => ReadOnly, value); }
        }

        public bool IsGift
        {
            get { return GetProperty(() => IsGift); }
            private set { SetProperty(() => IsGift, value); }
        }

        public bool AssemblyIncluded
        {
            get { return GetProperty(() => AssemblyIncluded); }
            set { SetProperty(() => AssemblyIncluded, value); }
        }

        public int CategoryTypeId => Category.TypeId ?? CategoryType.Other.Id;

        public int[] SupportedCategoryIds { get; }

        public NomenclatureViewItem Product
        {
            get { return GetProperty(() => Product); }
            private set { SetProperty(() => Product, value, ProductChanged); }
        }

        public string ProductName => Product?.Name;

        public static void BuildMetadata(MetadataBuilder<AssemblyViewItem> builder)
        {
            builder.Property(x => x.ProductName)
                .MatchesInstanceRule((x, y) => x == null || y.SupportedCategoryIds.Contains(y.Product.CategoryId), () => "У товара другая родительская категория");
        }

        public void SetProduct(NomenclatureViewItem product)
        {
            Product = product;
        }

        public void RemoveProduct()
        {
            Product = null;
        }

        private void ProductChanged()
        {
            RaisePropertiesChanged(nameof(ProductName));
        }
    }
}
