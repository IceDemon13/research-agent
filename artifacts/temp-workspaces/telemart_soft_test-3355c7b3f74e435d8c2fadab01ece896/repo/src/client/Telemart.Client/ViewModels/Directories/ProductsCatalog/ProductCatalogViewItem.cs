using System;
using System.ComponentModel;
using DevExpress.Mvvm;
using Telemart.Client.Common;

namespace Telemart.Client.ViewModels.Directories.ProductsCatalog
{
    public class ProductCatalogViewItem : BindableBase, IDataErrorInfo, IUniqueItem
    {
        public int Id
        {
            get { return GetProperty(() => Id); }
            set { SetProperty(() => Id, value, RaiseProperties); }
        }

        public int CategoryId
        {
            get { return GetProperty(() => CategoryId); }
            set { SetProperty(() => CategoryId, value); }
        }

        public int? BasedOn
        {
            get { return GetProperty(() => BasedOn); }
            set { SetProperty(() => BasedOn, value); }
        }

        public string Manufactor
        {
            get { return GetProperty(() => Manufactor); }
            set { SetProperty(() => Manufactor, value, () => RaisePropertyChanged(nameof(Name))); }
        }

        public string PrefixRus
        {
            get { return GetProperty(() => PrefixRus); }
            set { SetProperty(() => PrefixRus, value); }
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

        public string Name
        {
            get { return GetProperty(() => Name); }
            set { SetProperty(() => Name, value, () => RaisePropertiesChanged(nameof(Model), nameof(Color), nameof(Manufactor))); }
        }

        public string NameUkr
        {
            get { return GetProperty(() => NameUkr); }
            set { SetProperty(() => NameUkr, value, () => RaisePropertiesChanged(nameof(ModelUkr), nameof(Color), nameof(Manufactor))); }
        }

        public string NameEn
        {
            get { return GetProperty(() => NameEn); }
            set { SetProperty(() => NameEn, value, () => RaisePropertiesChanged(nameof(ModelEn), nameof(Color), nameof(Manufactor))); }
        }

        public string Model
        {
            get { return GetProperty(() => Model); }
            set { SetProperty(() => Model, value, () => RaisePropertiesChanged(nameof(Name), nameof(Color))); }
        }

        public string ModelUkr
        {
            get { return GetProperty(() => ModelUkr); }
            set { SetProperty(() => ModelUkr, value, () => RaisePropertiesChanged(nameof(NameUkr), nameof(Color))); }
        }

        public string ModelEn
        {
            get { return GetProperty(() => ModelEn); }
            set { SetProperty(() => ModelEn, value, () => RaisePropertiesChanged(nameof(NameEn), nameof(Color))); }
        }

        public string Modific
        {
            get { return GetProperty(() => Modific); }
            set { SetProperty(() => Modific, value); }
        }

        public string Color
        {
            get { return GetProperty(() => Color); }
            set { SetProperty(() => Color, value, () => RaisePropertiesChanged(nameof(Model), nameof(ModelUkr), nameof(ModelEn), nameof(Name), nameof(ColorPrimary))); }
        }

        public int? ColorPrimaryId
        {
            get { return GetProperty(() => ColorPrimaryId); }
            set { SetProperty(() => ColorPrimaryId, value); }
        }

        public ProductColorViewItem ColorPrimary
        {
            get { return GetProperty(() => ColorPrimary); }
            set { SetProperty(() => ColorPrimary, value, () => ColorPrimaryId = ColorPrimary?.Id); }
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

        public string PartNumber
        {
            get { return GetProperty(() => PartNumber); }
            set { SetProperty(() => PartNumber, value, () => RaisePropertyChanged(nameof(Keywords))); }
        }

        public string Keywords
        {
            get { return GetProperty(() => Keywords); }
            set { SetProperty(() => Keywords, value, () => RaisePropertyChanged(nameof(PartNumber))); }
        }

        public string YandexId
        {
            get { return GetProperty(() => YandexId); }
            set { SetProperty(() => YandexId, value); }
        }

        public int WarrantyRetailId
        {
            get { return GetProperty(() => WarrantyRetailId); }
            set { SetProperty(() => WarrantyRetailId, value); }
        }

        public int WarrantyWholesaleId
        {
            get { return GetProperty(() => WarrantyWholesaleId); }
            set { SetProperty(() => WarrantyWholesaleId, value); }
        }

        public int? WarrantyTypeId
        {
            get { return GetProperty(() => WarrantyTypeId); }
            set { SetProperty(() => WarrantyTypeId, value); }
        }

        public int? TypeId
        {
            get { return GetProperty(() => TypeId); }
            set { SetProperty(() => TypeId, value); }
        }

        public int? AssembledComputerRuleBaseProductId
        {
            get { return GetProperty(() => AssembledComputerRuleBaseProductId); }
            set { SetProperty(() => AssembledComputerRuleBaseProductId, value); }
        }

        public DateTime CreatedOn
        {
            get { return GetProperty(() => CreatedOn); }
            set { SetProperty(() => CreatedOn, value); }
        }

        public bool IsNo
        {
            get { return GetProperty(() => IsNo); }
            set { SetProperty(() => IsNo, value); }
        }

        public double Active
        {
            get { return GetProperty(() => Active); }
            set { SetProperty(() => Active, value, RaiseProperties); }
        }

        public bool? ActiveExpected
        {
            get { return GetProperty(() => ActiveExpected); }
            set { SetProperty(() => ActiveExpected, value); }
        }

        public DateTime? ActivatedOn
        {
            get { return GetProperty(() => ActivatedOn); }
            set { SetProperty(() => ActivatedOn, value, RaiseProperties); }
        }

        public string GroupName
        {
            get { return GetProperty(() => GroupName); }
            set { SetProperty(() => GroupName, value); }
        }

        public int? GroupFeatureId
        {
            get { return GetProperty(() => GroupFeatureId); }
            set { SetProperty(() => GroupFeatureId, value); }
        }

        public string Error => string.Empty;

        string IDataErrorInfo.this[string columnName] => ValidateColumn(columnName);

        protected virtual string ValidateColumn(string columnName)
        {
            return IDataErrorInfoHelper.GetErrorText(this, columnName);
        }

        protected virtual void RaiseProperties()
        {
        }
    }
}