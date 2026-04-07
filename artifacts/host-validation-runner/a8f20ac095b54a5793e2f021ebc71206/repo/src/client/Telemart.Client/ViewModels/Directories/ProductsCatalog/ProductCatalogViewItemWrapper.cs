using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text.RegularExpressions;
using Telemart.Client.Common;
using Telemart.Client.Core.Cloning;
using Telemart.Client.Dictionaries;
using Telemart.Client.Helpers;
using Telemart.Client.Properties;
using Telemart.Client.ViewModels.Directories.Category;

namespace Telemart.Client.ViewModels.Directories.ProductsCatalog
{
    public sealed class ProductCatalogViewItemWrapper : ProductCatalogViewItem, ICloneable
    {
        private const double ActiveValue = 1;
        private const double SemiActiveValue = 0.5;
        private const double NotActiveValue = 0;

        private readonly List<string> membersToIgnore = new List<string>
        {
            nameof(Error),
            nameof(Category),
            nameof(IsEdited),
            nameof(IsEditingOvertimed),
            nameof(ColorPrimary),
            nameof(ColorSecondary)
        };

        private readonly ProductCatalogViewItem original;

        private readonly ObjectChangeTracker<ProductCatalogViewItem, ProductCatalogViewItem> changeTracker;

        private bool initialized;
        private int cloneCounter;
        private CategoryProductCatalogViewItem excludedCategory;
        private int[] supportedFeatureIds;

        public ProductCatalogViewItemWrapper(ProductCatalogViewItem item)
        {
            original = item;

            changeTracker = new ObjectChangeTracker<ProductCatalogViewItem, ProductCatalogViewItem>(this, original, membersToIgnore);
        }

        public ProductCatalogViewItemWrapper()
        {
        }

        public CategoryProductCatalogViewItem Category
        {
            get { return GetProperty(() => Category); }
            private set { SetProperty(() => Category, value, RaiseProperties); }
        }

        public bool IsEdited
        {
            get { return GetProperty(() => IsEdited); }
            private set { SetProperty(() => IsEdited, value); }
        }

        public ProductCatalogActivityState ActivityState
        {
            get
            {
                if (IsNew)
                {
                    return null;
                }

                ProductCatalogActivityState state = null;

                switch (Active)
                {
                    case ActiveValue:
                        state = ProductCatalogActivityState.Activated;
                        break;
                    case SemiActiveValue:
                        state = Math.Abs(Category.Active - ActiveValue) < 0.001 && ActivatedOn == null
                            ? ProductCatalogActivityState.WaitingForActivation
                            : ProductCatalogActivityState.VisibleInB2B;
                        break;
                    case NotActiveValue:
                        state = ProductCatalogActivityState.Hidden;
                        break;
                }

                return state;
            }
        }

        public Guid Guid { get; private set; }

        public bool IsEditingOvertimed => ProductHelper.IsProductEditingOvertimed(CreatedOn);

        public bool IsNew => Id == default;

        public void SetSupportedFeatures(int[] featureIds)
        {
            supportedFeatureIds = featureIds;
        }

        public void Initialize()
        {
            if (!initialized)
            {
                Guid = Guid.NewGuid();

                if (IsNew)
                {
                    ActiveExpected = Category.Active.Equals(0.5) ? null : (bool?)Category.Active.Equals(1);
                }

                initialized = true;

                PropertyChanged += OnPropertyChanged;
            }
        }

        object ICloneable.Clone()
        {
            return Clone();
        }

        public ProductCatalogViewItemWrapper Clone()
        {
            ProductCatalogViewItemWrapper clone = ReflectionObjectCloner.Clone(this, () => new ProductCatalogViewItemWrapper(original));
            clone.Id = 0;

            if (!string.IsNullOrEmpty(clone.Name))
            {
                clone.Name = $"{clone.Name} - Копия({++cloneCounter})";
            }

            InitializeClonedItem(clone);

            return clone;
        }

        public ProductCatalogViewItemWrapper CopyFromParentCategory()
        {
            ProductCatalogViewItemWrapper item = new ProductCatalogViewItemWrapper(new ProductCatalogViewItem())
            {
                Active = Active,
                Category = Category,
                CategoryId = CategoryId
            };

            item.SetSupportedFeatures(supportedFeatureIds);

            InitializeClonedItem(item);

            item.ResetToParentCategory();

            return item;
        }

        public void ResetToParentCategory()
        {
            Manufactor = Category.Manufactor;
            PrefixRus = Category.PrefixRus;
            PrefixUkr = Category.PrefixUkr;
            PrefixEn = Category.PrefixEn;
            WarrantyRetailId = Category.WarrantyRetailId;
            WarrantyWholesaleId = Category.WarrantyWholesaleId;
            WarrantyTypeId = Category.WarrantyTypeId;
        }

        public void ResetToParentCategoryIfEmpty()
        {
            Manufactor = string.IsNullOrEmpty(Manufactor) ? Category.Manufactor : Manufactor;
            PrefixRus = string.IsNullOrEmpty(PrefixRus) ? Category.PrefixRus : PrefixRus;
            PrefixUkr = string.IsNullOrEmpty(PrefixUkr) ? Category.PrefixUkr : PrefixUkr;
            PrefixEn = string.IsNullOrEmpty(PrefixEn) ? Category.PrefixEn : PrefixEn;
            WarrantyRetailId = WarrantyRetailId == 0 ? Category.WarrantyRetailId : WarrantyRetailId;
            WarrantyWholesaleId = WarrantyWholesaleId == 0 ? Category.WarrantyWholesaleId : WarrantyWholesaleId;
            WarrantyTypeId = WarrantyTypeId == 0 ? Category.WarrantyTypeId : WarrantyTypeId;
        }

        public void SetCategory(IReadOnlyDictionary<int, CategoryProductCatalogViewItem> categories, CategoryProductCatalogViewItem excludedCategory)
        {
            categories.TryGetValue(CategoryId, out CategoryProductCatalogViewItem categoryItem);
            Category = categoryItem;
            this.excludedCategory = excludedCategory;
        }

        public void SetCategory(CategoryProductCatalogViewItem category)
        {
            Category = category;
            CategoryId = category.Id;
            RaisePropertyChanged(nameof(Category));
        }

        protected override void RaiseProperties()
        {
            RaisePropertyChanged(nameof(ActivityState));
        }

        protected override string ValidateColumn(string columnName)
        {
            string error = null;
            bool ignoreColor = excludedCategory != null && Category.Left >= excludedCategory.Left && Category.Left <= excludedCategory.Right;

            switch (columnName)
            {
                case nameof(Manufactor):
                    error = GetErrorMessage(
                        Required(Manufactor),
                        MinLength(Manufactor, 2),
                        MaxLength(Manufactor, 100),
                        NameContainsManufactor(Name, Manufactor));
                    break;
                case nameof(Name):
                    error = GetErrorMessage(
                        Required(Name),
                        MinLength(Name, 5),
                        MaxLength(Name, 150),
                        ContainsNoIfNonOfficial(Name, IsNo),
                        NameContainsManufactor(Name, Manufactor),
                        NameModelColorCombination(Name, Model, Color, BasedOn, PartNumber, ignoreColor));
                    break;
                case nameof(NameUkr):
                    error = GetErrorMessage(
                        Required(NameUkr),
                        MinLength(NameUkr, 5),
                        MaxLength(NameUkr, 150),
                        ContainsNoIfNonOfficial(NameUkr, IsNo),
                        NameContainsManufactor(NameUkr, Manufactor),
                        NameModelColorCombination(NameUkr, ModelUkr, Color, BasedOn, PartNumber, ignoreColor));
                    break;
                case nameof(NameEn):
                    error = GetErrorMessage(
                        Required(NameEn),
                        MinLength(NameEn, 5),
                        MaxLength(NameEn, 150),
                        ContainsNoIfNonOfficial(NameEn, IsNo),
                        NameContainsManufactor(NameEn, Manufactor),
                        NameModelColorCombination(NameEn, ModelEn, Color, BasedOn, PartNumber, ignoreColor));
                    break;
                case nameof(Model):
                    error = GetErrorMessage(
                        Required(Model),
                        MinLength(Model, 5),
                        MaxLength(Model, 150),
                        NameModelColorCombination(Name, Model, Color, BasedOn, PartNumber, ignoreColor));
                    break;
                case nameof(ModelUkr):
                    error = GetErrorMessage(
                        Required(ModelUkr),
                        MinLength(ModelUkr, 5),
                        MaxLength(ModelUkr, 150),
                        NameModelColorCombination(NameUkr, ModelUkr, Color, BasedOn, PartNumber, ignoreColor));
                    break;
                case nameof(ModelEn):
                    error = GetErrorMessage(
                        Required(ModelEn),
                        MinLength(ModelEn, 5),
                        MaxLength(ModelEn, 150),
                        NameModelColorCombination(NameEn, ModelEn, Color, BasedOn, PartNumber, ignoreColor));
                    break;
                case nameof(Color):
                    error = GetErrorMessage(
                        MinLength(Color, 3),
                        MaxLength(Color, 100),
                        AllowCharsRegex(Color, @"^[a-zA-Zа-яА-Я\-&/ ]+$"),
                        ignoreColor ? string.Empty : NameModelColorCombination(Name, Model, Color, BasedOn, PartNumber, false),
                        !string.IsNullOrEmpty(Color) ? Required(ColorPrimaryId) : string.Empty);
                    break;
                case nameof(PartNumber):
                    error = GetErrorMessage(
                        MinLength(PartNumber, 4),
                        MaxLength(PartNumber, 50),
                        AllowCharsRegex(PartNumber, @"^[a-zA-Z0-9\-_\/\.\s\+()]+$"),
                        PartNumberRequired(PartNumber, Category?.KeepPn ?? false),
                        PnOrKeywordsRequired(Keywords, PartNumber));
                    break;
                case nameof(Keywords):
                    error = GetErrorMessage(
                        MinLength(Keywords, 3),
                        MaxLength(Keywords, 50),
                        PnOrKeywordsRequired(Keywords, PartNumber));
                    break;
                case nameof(YandexId):
                    error = GetErrorMessage(
                        MinLength(YandexId, 1),
                        MaxLength(YandexId, 11),
                        AllowCharsRegex(YandexId, @"^\d+$"));
                    break;
                case nameof(ActiveExpected):
                    if (IsNew)
                    {
                        error = GetErrorMessage(
                            ActiveLessThanCategoryActive(ActiveExpected.HasValue ? ActiveExpected.Value ? 1 : 0 : 0.5, Category.Active));
                    }

                    break;
                case nameof(ColorPrimary):
                case nameof(ColorPrimaryId):
                    if (!string.IsNullOrEmpty(Color))
                    {
                        error = GetErrorMessage(Required(ColorPrimaryId));
                    }

                    break;
                case nameof(GroupName):
                    error = GetErrorMessage(MaxLength(GroupName, 250));
                    break;
                case nameof(GroupFeatureId):
                    error = GetErrorMessage(
                        ValidGroupFeature(GroupFeatureId));
                    break;
                case nameof(TypeId):
                    error = GetErrorMessage(Required(TypeId));
                    break;
                default:
                    error = base.ValidateColumn(columnName);
                    break;
            }

            return error;
        }

        private static string JoinModelAndColor(string model, string color)
        {
            return string.Join(" ", model, color).Trim();
        }

        private static string GetError(bool isValid, string errorMessage)
        {
            return isValid ? string.Empty : errorMessage;
        }

        private static string AllowCharsRegex(string value, string regexPattern)
        {
            return GetError(string.IsNullOrEmpty(value) || Regex.IsMatch(value, regexPattern), "Значение содержит невалидные символы");
        }

        private void InitializeClonedItem(ProductCatalogViewItemWrapper item)
        {
            item.Initialize();

            item.CreatedOn = DateTime.Now;
            item.Id = 0;
            item.excludedCategory = excludedCategory;
            item.supportedFeatureIds = supportedFeatureIds;
            Guid = Guid.NewGuid();

            RaisePropertiesChanged(nameof(IsNew), nameof(IsEditingOvertimed));
        }

        private void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (!initialized || membersToIgnore.Contains(e.PropertyName))
            {
                return;
            }

            IsEdited = changeTracker.IsChanged;
        }

        #region Validation Rules

        private string Required(string value)
        {
            return string.IsNullOrEmpty(value) ? Resources.RequiredErrorMessage : string.Empty;
        }

        private string Required(int? value)
        {
            return !value.HasValue ? Resources.RequiredErrorMessage : string.Empty;
        }

        private string PartNumberRequired(string pn, bool keepPn)
        {
            bool isValid = !keepPn || !string.IsNullOrEmpty(pn);
            return GetError(isValid, Resources.RequiredErrorMessage);
        }

        private string MinLength(string value, int minLength)
        {
            int valueLength = value?.Length ?? minLength;
            bool isValid = valueLength >= minLength || valueLength == 0;
            return GetError(isValid, $"Содержит менее {minLength} символов");
        }

        private string MaxLength(string value, int maxLength)
        {
            int valueLength = value?.Length ?? maxLength;
            bool isValid = valueLength <= maxLength || valueLength == 0;
            return GetError(isValid, $"Содержит более {maxLength} символов");
        }

        private string NameContainsManufactor(string name, string manufactor)
        {
            bool invalid = !string.IsNullOrEmpty(manufactor) && !manufactor.Equals("NoName") && name?.IndexOf(manufactor, StringComparison.Ordinal) == -1;
            return GetError(!invalid, "Название не содержит производителя");
        }

        private string ValidGroupFeature(int? groupFeatureId)
        {
            bool invalid = groupFeatureId.HasValue && !supportedFeatureIds.Contains(groupFeatureId.Value);
            return GetError(!invalid, "Характеристика не допустима для категории");
        }

        private string ContainsNoIfNonOfficial(string name, bool isNo)
        {
            bool isValid = !isNo || name.Contains("n/o");
            return GetError(isValid, "Название должно содержать n/o");
        }

        private string ActiveLessThanCategoryActive(double productActive, double categoryActive)
        {
            bool isValid = productActive <= categoryActive;
            return GetError(isValid, "Active не может быть больше active категории");
        }

        private string NameModelColorCombination(string name, string model, string color, int? idBasedOn, string partNumber, bool ignoreColor)
        {
            model = model ?? string.Empty;

            if (idBasedOn.HasValue || string.IsNullOrEmpty(name) || name == JoinModelAndColor(model, color))
            {
                return string.Empty;
            }

            /// if (Category.Left >= excludedCategory.Left && Category.Left <= excludedCategory.Right)
            if (ignoreColor)
            {
                return name.StartsWith(model) ? string.Empty : "Ошибка в комбинации «Название, Модель»";
            }

            string noPart = " n/o";
            string sanitizedName = name.Replace(noPart, string.Empty);
            string sanitizedModel = model.Replace(noPart, string.Empty);

            if (!string.IsNullOrEmpty(partNumber))
            {
                sanitizedName = sanitizedName.Replace(PartNumber, string.Empty).Replace("()", string.Empty).Replace("  ", " ");
            }

            return sanitizedName == JoinModelAndColor(sanitizedModel, color)
                ? string.Empty
                : "Ошибка в комбинации «Название, Модель, Цвет»";
        }

        private string PnOrKeywordsRequired(string keywords, string partNumber)
        {
            bool isValid = !(string.IsNullOrEmpty(keywords) && string.IsNullOrEmpty(partNumber));
            return GetError(isValid, "PN и Ключ пустые");
        }

        private string GetErrorMessage(params string[] errors)
        {
            return string.Join(Environment.NewLine, errors.Where(x => !string.IsNullOrEmpty(x)));
        }

        #endregion
    }
}