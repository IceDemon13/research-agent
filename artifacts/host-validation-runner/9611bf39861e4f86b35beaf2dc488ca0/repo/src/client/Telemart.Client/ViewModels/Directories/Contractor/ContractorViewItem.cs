using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text.RegularExpressions;
using DevExpress.Mvvm;
using DevExpress.Mvvm.DataAnnotations;
using DevExpress.Mvvm.Native;
using Telemart.Client.Common.MvvmEnhancements;
using Telemart.Client.Common.Validation;
using Telemart.Client.Core.Cloning;
using Telemart.Client.Dictionaries;
using Telemart.Client.Properties;
using Telemart.Client.ViewModels.Base;
using Telemart.Client.ViewModels.Directories.Contractor.ParserSettings;

namespace Telemart.Client.ViewModels.Directories.Contractor
{
    public sealed class ContractorViewItem : BindableBase, IDataErrorInfo, ICloneable, ILockableEntity
    {
        public AbcType AbcType
        {
            get { return GetProperty(() => AbcType); }
            set { SetProperty(() => AbcType, value); }
        }

        public bool Active
        {
            get { return GetProperty(() => Active); }
            set { SetProperty(() => Active, value, () => RaisePropertyChanged(nameof(Employee))); }
        }

        public int? CityId
        {
            get { return GetProperty(() => CityId); }
            set { SetProperty(() => CityId, value); }
        }

        public int? CountryId
        {
            get { return GetProperty(() => CountryId); }
            set { SetProperty(() => CountryId, value); }
        }

        public string TelegramChatId
        {
            get { return GetProperty(() => TelegramChatId); }
            set { SetProperty(() => TelegramChatId, value); }
        }

        public string Comment
        {
            get { return GetProperty(() => Comment); }
            set { SetProperty(() => Comment, value); }
        }

        public int? EmployeeId
        {
            get { return GetProperty(() => EmployeeId); }
            set { SetProperty(() => EmployeeId, value); }
        }

        public decimal? Discount
        {
            get { return GetProperty(() => Discount); }
            set { SetProperty(() => Discount, value); }
        }

        public short GracePeriod
        {
            get { return GetProperty(() => GracePeriod); }
            set { SetProperty(() => GracePeriod, value); }
        }

        public short ReturnPeriod
        {
            get { return GetProperty(() => ReturnPeriod); }
            set { SetProperty(() => ReturnPeriod, value); }
        }

        public ComboBoxItem? Employee
        {
            get { return GetProperty(() => Employee); }
            set { SetProperty(() => Employee, value, () => EmployeeId = Employee?.Id); }
        }

        public int? EmployeeLockId
        {
            get { return GetProperty(() => EmployeeLockId); }
            set { SetProperty(() => EmployeeLockId, value); }
        }

        public int Id
        {
            get { return GetProperty(() => Id); }
            set { SetProperty(() => Id, value, RaisePropertiesChanged); }
        }

        public int? ParseFeaturesPriority
        {
            get { return GetProperty(() => ParseFeaturesPriority); }
            set { SetProperty(() => ParseFeaturesPriority, value, RaisePropertiesChanged); }
        }

        public bool IsClient
        {
            get { return GetProperty(() => IsClient); }
            set { SetProperty(() => IsClient, value, RaisePropertiesChanged); }
        }

        public bool IsCompetitor
        {
            get { return GetProperty(() => IsCompetitor); }
            set { SetProperty(() => IsCompetitor, value, RaisePropertiesChanged); }
        }

        public bool IsFolder
        {
            get { return GetProperty(() => IsFolder); }
            set { SetProperty(() => IsFolder, value, RaisePropertiesChanged); }
        }

        public bool IsRetail
        {
            get { return GetProperty(() => IsRetail); }
            set { SetProperty(() => IsRetail, value); }
        }

        public bool ParseFeatures
        {
            get { return GetProperty(() => ParseFeatures); }
            set { SetProperty(() => ParseFeatures, value); }
        }

        public bool IsSupplier
        {
            get { return GetProperty(() => IsSupplier); }
            set { SetProperty(() => IsSupplier, value, RaisePropertiesChanged); }
        }

        public bool AutoSource
        {
            get { return GetProperty(() => AutoSource); }
            set { SetProperty(() => AutoSource, value, RaisePropertiesChanged); }
        }

        public bool CreatedIn1C
        {
            get { return GetProperty(() => CreatedIn1C); }
            set { SetProperty(() => CreatedIn1C, value); }
        }

        public bool IsIndividual
        {
            get { return GetProperty(() => IsIndividual); }
            set { SetProperty(() => IsIndividual, value, () => RaisePropertyChanged(nameof(Edrpou))); }
        }

        public string Name
        {
            get { return GetProperty(() => Name); }
            set { SetProperty(() => Name, value); }
        }

        public int? ParentId
        {
            get { return GetProperty(() => ParentId); }
            set { SetProperty(() => ParentId, value); }
        }

        public Subdivision Subdivision
        {
            get { return GetProperty(() => Subdivision); }
            set { SetProperty(() => Subdivision, value); }
        }

        public int PriceTypeId
        {
            get { return GetProperty(() => PriceTypeId); }
            set { SetProperty(() => PriceTypeId, value); }
        }

        public string Url
        {
            get { return GetProperty(() => Url); }
            set { SetProperty(() => Url, value); }
        }

        public string EmployeeLockName
        {
            get { return GetProperty(() => EmployeeLockName); }
            set { SetProperty(() => EmployeeLockName, value); }
        }

        public int Limit
        {
            get { return GetProperty(() => Limit); }
            set { SetProperty(() => Limit, value); }
        }

        public bool IsServiceSupplier
        {
            get { return GetProperty(() => IsServiceSupplier); }
            set { SetProperty(() => IsServiceSupplier, value); }
        }

        public string Edrpou
        {
            get { return GetProperty(() => Edrpou); }
            set { SetProperty(() => Edrpou, value); }
        }

        public bool CurrencyManual
        {
            get { return GetProperty(() => CurrencyManual); }
            set { SetProperty(() => CurrencyManual, value); }
        }

        public bool OldClient
        {
            get { return GetProperty(() => OldClient); }
            set { SetProperty(() => OldClient, value); }
        }

        public int? Buh1CId
        {
            get { return GetProperty(() => Buh1CId); }
            set { SetProperty(() => Buh1CId, value); }
        }

        public ObservableCollection<ContractorCurrencyPermissionViewItem> CurrencyPermissions
        {
            get { return GetProperty(() => CurrencyPermissions); }
            set { SetProperty(() => CurrencyPermissions, value, () => RaisePropertyChanged(nameof(IsAutoSourceEnabled))); }
        }

        public ObservableCollection<ParserSettingsViewItem> ParserSettings
        {
            get { return GetProperty(() => ParserSettings); }
            set { SetProperty(() => ParserSettings, value); }
        }

        public int? OwnershipFormId
        {
            get { return GetProperty(() => OwnershipFormId); }
            set { SetProperty(() => OwnershipFormId, value); }
        }

        public string ActualAddress
        {
            get { return GetProperty(() => ActualAddress); }
            set { SetProperty(() => ActualAddress, value); }
        }

        public string LegalAddress
        {
            get { return GetProperty(() => LegalAddress); }
            set { SetProperty(() => LegalAddress, value); }
        }

        public string Tin
        {
            get { return GetProperty(() => Tin); }
            set { SetProperty(() => Tin, value); }
        }

        public bool VatAllowed
        {
            get { return GetProperty(() => VatAllowed); }
            set { SetProperty(() => VatAllowed, value); }
        }

        public bool AllowDocuments
        {
            get { return GetProperty(() => AllowDocuments); }
            set { SetProperty(() => AllowDocuments, value); }
        }

        public bool RetailWarranty
        {
            get { return GetProperty(() => RetailWarranty); }
            set { SetProperty(() => RetailWarranty, value); }
        }

        public string Organization
        {
            get { return GetProperty(() => Organization); }
            set { SetProperty(() => Organization, value); }
        }

        public string PurchaseProcessorName
        {
            get { return GetProperty(() => PurchaseProcessorName); }
            set { SetProperty(() => PurchaseProcessorName, value, () => RaisePropertiesChanged(nameof(PurchaseLogin), nameof(PurchasePassword))); }
        }

        public string PurchaseLogin
        {
            get { return GetProperty(() => PurchaseLogin); }
            set { SetProperty(() => PurchaseLogin, value); }
        }

        public string PurchasePassword
        {
            get { return GetProperty(() => PurchasePassword); }
            set { SetProperty(() => PurchasePassword, value); }
        }

        public bool PurchaseAutoReserve
        {
            get { return GetProperty(() => PurchaseAutoReserve); }
            set { SetProperty(() => PurchaseAutoReserve, value, () => PurchaseCheckUnique = PurchaseAutoReserve && PurchaseCheckUnique); }
        }

        public bool PurchaseAutoPurchase
        {
            get { return GetProperty(() => PurchaseAutoPurchase); }
            set { SetProperty(() => PurchaseAutoPurchase, value); }
        }

        public bool PurchaseCheckUnique
        {
            get { return GetProperty(() => PurchaseCheckUnique); }
            set { SetProperty(() => PurchaseCheckUnique, value); }
        }

        public string TypeDisplayValue => IsFolder
            ? "Папка"
            : "Контрагент";

        public string ParseFeaturesPriorityStr => $"Приоритет характеристик{(ParseFeaturesPriority > 0 ? $" ({ParseFeaturesPriority})" : null)} [чем ниже значение, тем приоритетнее]";

        public bool IsNew => Id <= 0;

        public bool CanViewSettings => !IsNew && !IsFolder && !IsCompetitor;

        public bool CanViewContacts => !IsNew && !IsFolder && !IsCompetitor;

        public bool CanViewTemplates => !IsNew && !IsFolder && IsClient;

        public bool CanViewLogistics => !IsNew && !IsFolder && IsSupplier;

        public bool CanViewParsers => !IsNew && !IsFolder && (IsCompetitor || IsSupplier);

        public bool IsAutoSourceEnabled => CurrencyPermissions?.Any(x => x.CurrencyId == Currency.UahId && x.Sale) == true;

        #region IDataErrorInfo

        string IDataErrorInfo.Error => string.Empty;

        string IDataErrorInfo.this[string columnName] => IDataErrorInfoHelper.GetErrorText(this, columnName);

        #endregion

        public static void BuildMetadata(MetadataBuilder<ContractorViewItem> builder)
        {
            builder.Property(x => x.Name)
                .Required(() => Resources.RequiredErrorMessage)
                .MaxLength(ContractorConstants.NameMaxLength);

            builder.Property(x => x.Subdivision)
                .Required(() => Resources.RequiredErrorMessage);

            builder.Property(x => x.Employee)
                .RequiredActive(x => x.Active);

            builder.Property(x => x.CityId)
                .Required(() => Resources.RequiredErrorMessage);

            builder.Property(x => x.Url)
                .MatchesInstanceRule((x, _) => IsUriValid(x), () => "Cсылка не корректна");

            builder.Property(x => x.Comment)
                .MaxLength(ContractorConstants.CommentMaxLength);

            builder.Property(x => x.Organization)
                .MatchesInstanceRule((x, y) => !y.IsSupplier || !string.IsNullOrEmpty(x), () => Resources.RequiredErrorMessage);

            builder.Property(x => x.Edrpou)
               .MatchesInstanceRule((x, y) => !(x != null && x.Length != 10 && y.IsIndividual), () => "Длина ЕДРПОУ для физ. лица 10 символов")
               .MatchesInstanceRule((x, y) => !(x != null && x.Length != 8 && !y.IsIndividual), () => "Длина ЕДРПОУ для юр. лица 8 символов");

            builder.Property(x => x.PurchaseLogin)
                .MatchesInstanceRule((x, y) => string.IsNullOrWhiteSpace(y.PurchaseProcessorName) || !string.IsNullOrWhiteSpace(x), () => Resources.RequiredErrorMessage);

            builder.Property(x => x.PurchasePassword)
                .MatchesInstanceRule((x, y) => string.IsNullOrWhiteSpace(y.PurchaseProcessorName) || !string.IsNullOrWhiteSpace(x), () => Resources.RequiredErrorMessage);

            builder.Property(x => x.Buh1CId).MatchesRule(x => !x.HasValue || x > 0, () => "Значение должно быть больше 0");
        }

        object ICloneable.Clone()
        {
            return Clone();
        }

        public ContractorViewItem Clone()
        {
            ContractorViewItem item = ReflectionObjectCloner.Clone(this);

            item.CurrencyPermissions = CurrencyPermissions.Select(x =>
            {
                ContractorCurrencyPermissionViewItem viewItem = ReflectionObjectCloner.Clone(x);
                viewItem.Sale = x.Sale;
                viewItem.Purchase = x.Purchase;
                viewItem.CurrencyControl = x.CurrencyControl;
                return viewItem;
            }).ToObservableCollection();

            return item;
        }

        public void RaisePropertiesChanged()
        {
            RaisePropertiesChanged(
                nameof(IsNew),
                nameof(CanViewSettings),
                nameof(CanViewContacts),
                nameof(CanViewTemplates),
                nameof(CanViewLogistics),
                nameof(CanViewParsers),
                nameof(Organization),
                nameof(AutoSource),
                nameof(TypeDisplayValue),
                nameof(IsAutoSourceEnabled),
                nameof(CurrencyPermissions),
                nameof(ParseFeaturesPriorityStr));
        }

        private static bool IsUriValid(string uriString)
        {
            return string.IsNullOrWhiteSpace(uriString)
                   || (Uri.TryCreate(uriString, UriKind.Absolute, out Uri _) && Regex.IsMatch(uriString, @"^.+\..+$"));
        }

        public bool IncludePurchaseData() => !string.IsNullOrEmpty(PurchaseProcessorName)
            || !string.IsNullOrEmpty(PurchaseLogin)
            || !string.IsNullOrEmpty(PurchasePassword);
    }
}