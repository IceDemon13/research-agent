using System;
using System.Collections.ObjectModel;
using System.Linq;
using DevExpress.Mvvm.DataAnnotations;
using DevExpress.Mvvm.Native;
using Telemart.Client.Core.Cloning;
using Telemart.Client.Properties;
using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.PromoCode
{
    public class PromoCodeFullViewItem : PromoCodeViewItem, ICloneable, ILockableEntity
    {
        public PromoCodeFullViewItem()
        {
            Products = new ObservableCollection<PromoCodeProductViewItem>();
            BundleCategories = new ObservableCollection<PromoCodeBundleCategoryViewItem>();
            BundleProducts = new ObservableCollection<PromoCodeBundleProductViewItem>();
        }

        public int? EmployeeLockId
        {
            get { return GetProperty(() => EmployeeLockId); }
            set { SetProperty(() => EmployeeLockId, value); }
        }

        public string EmployeeLockName
        {
            get { return GetProperty(() => EmployeeLockName); }
            set { SetProperty(() => EmployeeLockName, value); }
        }

        public string Description
        {
            get { return GetProperty(() => Description); }
            set { SetProperty(() => Description, value); }
        }

        public string DescriptionUkr
        {
            get { return GetProperty(() => DescriptionUkr); }
            set { SetProperty(() => DescriptionUkr, value); }
        }

        public string DescriptionEn
        {
            get { return GetProperty(() => DescriptionEn); }
            set { SetProperty(() => DescriptionEn, value); }
        }

        public bool UsedInOrders
        {
            get { return GetProperty(() => UsedInOrders); }
            set { SetProperty(() => UsedInOrders, value); }
        }

        public ObservableCollection<PromoCodeProductViewItem> Products
        {
            get { return GetProperty(() => Products); }
            set { SetProperty(() => Products, value); }
        }

        public ObservableCollection<PromoCodeBundleCategoryViewItem> BundleCategories
        {
            get { return GetProperty(() => BundleCategories); }
            set { SetProperty(() => BundleCategories, value); }
        }

        public ObservableCollection<PromoCodeBundleProductViewItem> BundleProducts
        {
            get { return GetProperty(() => BundleProducts); }
            set { SetProperty(() => BundleProducts, value); }
        }

        public static void BuildMetadata(MetadataBuilder<PromoCodeFullViewItem> builder)
        {
            builder.Property(x => x.DateStart)
                .MatchesInstanceRule((x, y) => y.EmployeeLockId == null || y.DateEnd.HasValue == false || (x < y.DateEnd), () => "Дата начала должна быть меньше даты окончания");
            builder.Property(x => x.DateEnd)
                .MatchesInstanceRule((x, y) => y.EmployeeLockId == null || x >= DateTime.Now, () => "Дата окончания должна быть больше текущей даты");
            builder.Property(x => x.Description).Required(() => Resources.RequiredErrorMessage);
            builder.Property(x => x.DescriptionUkr).Required(() => Resources.RequiredErrorMessage);
            builder.Property(x => x.DescriptionEn).Required(() => Resources.RequiredErrorMessage);
        }

        object ICloneable.Clone()
        {
            return Clone();
        }

        protected virtual object Clone()
        {
            PromoCodeFullViewItem item = ReflectionObjectCloner.Clone(this);

            item.Products = Products.Select(x => ReflectionObjectCloner.Clone(x)).ToObservableCollection();
            item.BundleCategories = BundleCategories.Select(x => ReflectionObjectCloner.Clone(x)).ToObservableCollection();
            item.BundleProducts = BundleProducts.Select(x => ReflectionObjectCloner.Clone(x)).ToObservableCollection();

            return item;
        }
    }
}
