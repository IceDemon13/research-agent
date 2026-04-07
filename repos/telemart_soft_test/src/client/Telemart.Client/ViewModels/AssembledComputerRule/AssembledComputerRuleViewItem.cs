using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;
using DevExpress.Mvvm.DataAnnotations;
using DevExpress.Mvvm.Native;
using Telemart.Client.Core.Cloning;
using Telemart.Client.Properties;
using Telemart.Client.ViewModels.Base;
using Telemart.Client.ViewModels.Directories.ProductsCatalog;

namespace Telemart.Client.ViewModels.AssembledComputerRule
{
    public class AssembledComputerRuleViewItem : TelemartEditorViewItemBase
    {
        public AssembledComputerRuleViewItem()
        {
            Categories = new ObservableCollection<AssembledComputerRuleCategoryViewItem>();
            Products = new ObservableCollection<AssembledComputerRuleProductViewItem>();
            BuildModel();
        }

        public string Configuration
        {
            get { return GetProperty(() => Configuration); }
            set { SetProperty(() => Configuration, value, BuildModel); }
        }

        public string PartNumber
        {
            get { return GetProperty(() => PartNumber); }
            set { SetProperty(() => PartNumber, value, PartNumberChanged); }
        }

        public bool Active
        {
            get { return GetProperty(() => Active); }
            set { SetProperty(() => Active, value); }
        }

        public string Prefix
        {
            get { return GetProperty(() => Prefix); }
            set { SetProperty(() => Prefix, value, BuildModel); }
        }

        public string PrefixUkr
        {
            get { return GetProperty(() => PrefixUkr); }
            set { SetProperty(() => PrefixUkr, value); }
        }

        public string PrefixEn
        {
            get { return GetProperty(() => PrefixEn); }
            set { SetProperty(() => PrefixEn, value); }
        }

        public BrandViewItem? Brand
        {
            get { return GetProperty(() => Brand); }
            set { SetProperty(() => Brand, value, BrandChanged); }
        }

        public string Line
        {
            get { return GetProperty(() => Line); }
            set { SetProperty(() => Line, value, BuildModel); }
        }

        public string Series
        {
            get { return GetProperty(() => Series); }
            set { SetProperty(() => Series, value, BuildModel); }
        }

        public string Name
        {
            get { return GetProperty(() => Name); }
            set { SetProperty(() => Name, value); }
        }

        public string BaseProductName
        {
            get { return GetProperty(() => BaseProductName); }
            set { SetProperty(() => BaseProductName, value); }
        }

        public int? BaseProductId
        {
            get { return GetProperty(() => BaseProductId); }
            set { SetProperty(() => BaseProductId, value); }
        }

        public string Model
        {
            get { return GetProperty(() => Model); }
            set { SetProperty(() => Model, value); }
        }

        public string Modific
        {
            get { return GetProperty(() => Modific); }
            set { SetProperty(() => Modific, value); }
        }

        public int? ColorPrimaryId
        {
            get { return GetProperty(() => ColorPrimaryId); }
            set { SetProperty(() => ColorPrimaryId, value); }
        }

        public int? ColorSecondaryId
        {
            get { return GetProperty(() => ColorSecondaryId); }
            set { SetProperty(() => ColorSecondaryId, value); }
        }

        public ProductColorViewItem ColorSecondary
        {
            get { return GetProperty(() => ColorSecondary); }
            set { SetProperty(() => ColorSecondary, value, () => ColorSecondaryId = ColorSecondary?.Id); }
        }

        public ProductColorViewItem ColorPrimary
        {
            get { return GetProperty(() => ColorPrimary); }
            set { SetProperty(() => ColorPrimary, value, () => ColorPrimaryId = ColorPrimary?.Id); }
        }

        public string Color
        {
            get { return GetProperty(() => Color); }
            set { SetProperty(() => Color, value, ColorChanged); }
        }

        public ObservableCollection<AssembledComputerRuleCategoryViewItem> Categories
        {
            get { return GetProperty(() => Categories); }
            set { SetProperty(() => Categories, value); }
        }

        public ObservableCollection<AssembledComputerRuleProductViewItem> Products
        {
            get { return GetProperty(() => Products); }
            set { SetProperty(() => Products, value); }
        }

        public ObservableCollection<int> IgnoreSlotConsumerIds
        {
            get { return GetProperty(() => IgnoreSlotConsumerIds); }
            set { SetProperty(() => IgnoreSlotConsumerIds, value); }
        }

        public static void BuildMetadata(MetadataBuilder<AssembledComputerRuleViewItem> builder)
        {
            const string spaceError = "Поле не должно содержать пробелы";
            const string invalidValue = "Невалидное значение";

            builder.Property(x => x.Prefix)
                .Required(() => Resources.RequiredErrorMessage);
            builder.Property(x => x.PrefixUkr)
                .Required(() => Resources.RequiredErrorMessage);
            builder.Property(x => x.PrefixEn)
                .Required(() => Resources.RequiredErrorMessage);
            builder.Property(x => x.Name)
                .Required(() => Resources.RequiredErrorMessage);
            builder.Property(x => x.Series)
                .MatchesInstanceRule((x, y) => x is null || !x.Contains(' '), () => spaceError)
                .Required(() => Resources.RequiredErrorMessage);
            builder.Property(x => x.Line)
                .MatchesInstanceRule((x, y) => x is null || !x.Contains(' '), () => spaceError)
                .Required(() => Resources.RequiredErrorMessage);
            builder.Property(x => x.Color)
                .Required(() => Resources.RequiredErrorMessage)
                .MatchesInstanceRule((x, y) => string.IsNullOrWhiteSpace(x) || (!x.Contains(' ') && x.Length > 2 && Regex.IsMatch(x, @"^[a-zA-Zа-яА-Я\-&/ ]+$")), () => invalidValue);
            builder.Property(x => x.Brand)
                .Required(() => Resources.RequiredErrorMessage);
            builder.Property(x => x.PartNumber)
                .MatchesInstanceRule((x, y) => IsPartNumberValid(x), () => invalidValue);
            builder.Property(x => x.Configuration)
                .MatchesInstanceRule((x, y) => x is null || !x.Contains(' '), () => spaceError);
            builder.Property(x => x.Model)
                .MaxLength(150, () => "Максимально допустимая длина модели 150 символов")
                .Required(() => Resources.RequiredErrorMessage);
            builder.Property(x => x.ColorPrimary)
                .MatchesInstanceRule((x, y) => x != null || string.IsNullOrEmpty(y.Color), () => Resources.RequiredErrorMessage);
        }

        public override object Clone()
        {
            AssembledComputerRuleViewItem item = (AssembledComputerRuleViewItem)base.Clone();

            item.Categories = Categories.Select(x => ReflectionObjectCloner.Clone(x)).ToObservableCollection();
            item.Products = Products.Select(x => ReflectionObjectCloner.Clone(x)).ToObservableCollection();

            return item;
        }

        private static bool IsPartNumberValid(string partNumber)
        {
            return partNumber != null && Regex.IsMatch(partNumber, @"^[a-zA-Z0-9\-_\/\.\s\+()]+$") && partNumber.Length > 3;
        }

        private void BuildName()
        {
            Name = $"{(Model.Contains('[') ? "[модель]" : Model)} {(PartNumber is null ? "[артикул]" : $"({PartNumber})")}{(string.IsNullOrEmpty(Color) ? string.Empty : $" {Color}")}";
        }

        private void BuildModel()
        {
            Model = $"{Brand?.Name ?? "[бренд]"} {Line ?? "[линейка]"} {Series ?? "[серия]"}{(string.IsNullOrEmpty(Configuration) ? string.Empty : $" {Configuration}")}";

            BuildName();
        }

        private void PartNumberChanged()
        {
            BuildModel();

            if (IsPartNumberValid(PartNumber))
            {
                string[] splittedPartNumber = PartNumber.Split("-");

                Modific = splittedPartNumber.Length == 3 ? string.Join("-", splittedPartNumber.Take(2)) : PartNumber;
            }
            else
            {
                Modific = "[заполняется на основании артикула]";
            }
        }

        private void BrandChanged()
        {
            if (Id == 0)
            {
                if (string.IsNullOrWhiteSpace(Prefix))
                {
                    Prefix = Brand?.Prefix;
                }

                if (string.IsNullOrWhiteSpace(PrefixUkr))
                {
                    PrefixUkr = Brand?.PrefixUkr;
                }

                if (string.IsNullOrWhiteSpace(PrefixEn))
                {
                    PrefixEn = Brand?.PrefixEn;
                }
            }

            BuildModel();
        }

        private void ColorChanged()
        {
            BuildName();
            RaisePropertyChanged(nameof(ColorPrimary));
        }
    }
}