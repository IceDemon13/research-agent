using System.Collections.ObjectModel;
using System.Linq;
using DevExpress.Mvvm.DataAnnotations;
using DevExpress.Mvvm.Native;
using Telemart.Client.Properties;
using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.AdditionalService
{
    public class AdditionalServiceViewItem : TelemartEditorViewItemBase
    {
        public AdditionalServiceViewItem()
        {
            SlaveCategories = new ObservableCollection<AdditionalServiceSlaveCategoryViewItem>();
            ProvideRules = new ObservableCollection<AdditionalServiceProvideRuleItem>();
        }

        public string Name
        {
            get { return GetProperty(() => Name); }
            set { SetProperty(() => Name, value); }
        }

        public string NameUa
        {
            get { return GetProperty(() => NameUa); }
            set { SetProperty(() => NameUa, value); }
        }

        public string NameEn
        {
            get { return GetProperty(() => NameEn); }
            set { SetProperty(() => NameEn, value); }
        }

        public string Description
        {
            get { return GetProperty(() => Description); }
            set { SetProperty(() => Description, value); }
        }

        public string DescriptionUa
        {
            get { return GetProperty(() => DescriptionUa); }
            set { SetProperty(() => DescriptionUa, value); }
        }

        public string DescriptionEn
        {
            get { return GetProperty(() => DescriptionEn); }
            set { SetProperty(() => DescriptionEn, value); }
        }

        public string ProductName
        {
            get { return GetProperty(() => ProductName); }
            set { SetProperty(() => ProductName, value); }
        }

        public string GroupName
        {
            get { return GetProperty(() => GroupName); }
            set { SetProperty(() => GroupName, value); }
        }

        public int ProductId
        {
            get { return GetProperty(() => ProductId); }
            set { SetProperty(() => ProductId, value); }
        }

        public int GroupId
        {
            get { return GetProperty(() => GroupId); }
            set { SetProperty(() => GroupId, value); }
        }

        public int? ProductTypeId
        {
            get { return GetProperty(() => ProductTypeId); }
            set { SetProperty(() => ProductTypeId, value); }
        }

        public decimal MinPrice
        {
            get { return GetProperty(() => MinPrice); }
            set { SetProperty(() => MinPrice, value); }
        }

        public int? PriorityTypeId
        {
            get { return GetProperty(() => PriorityTypeId); }
            set { SetProperty(() => PriorityTypeId, value); }
        }

        public decimal Percent
        {
            get { return GetProperty(() => Percent); }
            set { SetProperty(() => Percent, value); }
        }

        public bool Active
        {
            get { return GetProperty(() => Active); }
            set { SetProperty(() => Active, value); }
        }

        public bool AdditionalWarranty
        {
            get { return GetProperty(() => AdditionalWarranty); }
            set { SetProperty(() => AdditionalWarranty, value); }
        }

        public bool RequireProductsToProvide
        {
            get { return GetProperty(() => RequireProductsToProvide); }
            set { SetProperty(() => RequireProductsToProvide, value); }
        }

        public bool AutoAdd
        {
            get { return GetProperty(() => AutoAdd); }
            set { SetProperty(() => AutoAdd, value); }
        }

        public bool ControlInMovements
        {
            get { return GetProperty(() => ControlInMovements); }
            set { SetProperty(() => ControlInMovements, value); }
        }

        public bool PresenceOfCustomer
        {
            get { return GetProperty(() => PresenceOfCustomer); }
            set { SetProperty(() => PresenceOfCustomer, value); }
        }

        public bool IsLocal
        {
            get { return GetProperty(() => IsLocal); }
            set { SetProperty(() => IsLocal, value); }
        }

        public bool AssemblyPart
        {
            get { return GetProperty(() => AssemblyPart); }
            set { SetProperty(() => AssemblyPart, value); }
        }

        public bool CreateDiscount
        {
            get { return GetProperty(() => CreateDiscount); }
            set { SetProperty(() => CreateDiscount, value); }
        }

        public bool Disassembly
        {
            get { return GetProperty(() => Disassembly); }
            set { SetProperty(() => Disassembly, value); }
        }

        public ObservableCollection<int> PriceIds
        {
            get { return GetProperty(() => PriceIds); }
            set { SetProperty(() => PriceIds, value); }
        }

        public ObservableCollection<AdditionalServiceSlaveCategoryViewItem> SlaveCategories
        {
            get { return GetProperty(() => SlaveCategories); }
            set { SetProperty(() => SlaveCategories, value); }
        }

        public ObservableCollection<AdditionalServiceProvideRuleItem> ProvideRules
        {
            get { return GetProperty(() => ProvideRules); }
            set { SetProperty(() => ProvideRules, value); }
        }

        public static void BuildMetadata(MetadataBuilder<AdditionalServiceViewItem> builder)
        {
            builder.Property(x => x.ProductName)
                .Required(() => Resources.RequiredErrorMessage);
            builder.Property(x => x.Name)
                .Required(() => Resources.RequiredErrorMessage);
            builder.Property(x => x.NameUa)
                .Required(() => Resources.RequiredErrorMessage);
            builder.Property(x => x.NameEn)
                .Required(() => Resources.RequiredErrorMessage);
            builder.Property(x => x.Description)
                .Required(() => Resources.RequiredErrorMessage);
            builder.Property(x => x.DescriptionUa)
                .Required(() => Resources.RequiredErrorMessage);
            builder.Property(x => x.DescriptionEn)
                .Required(() => Resources.RequiredErrorMessage);
            builder.Property(x => x.MinPrice)
                .MatchesInstanceRule((x, y) => x >= 1, () => "Минимальная цена услуги 1 грн");
            builder.Property(x => x.Percent)
                .MatchesInstanceRule((x, y) => x >= 0, () => "Значение не может быть отрицательным");
            builder.Property(x => x.ProductTypeId)
                .Required(() => Resources.RequiredErrorMessage);
            builder.Property(x => x.PriorityTypeId)
               .Required(() => Resources.RequiredErrorMessage);
            builder.Property(x => x.PriceIds)
               .MatchesRule(x => x?.Any() == true, () => Resources.RequiredErrorMessage);
        }

        public override object Clone()
        {
            AdditionalServiceViewItem item = (AdditionalServiceViewItem)base.Clone();
            item.SlaveCategories = SlaveCategories.ToObservableCollection();
            item.ProvideRules = ProvideRules.ToObservableCollection();

            return item;
        }
    }
}