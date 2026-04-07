using System;
using System.ComponentModel;
using DevExpress.Mvvm;
using DevExpress.Mvvm.DataAnnotations;
using Telemart.Client.Core.Cloning;
using Telemart.Client.Dictionaries;
using Telemart.Client.Properties;
using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.Directories.Organization
{
    public class OrganizationViewItem : BindableBase, IDataErrorInfo, ICloneable, ILockableEntity
    {
        public int Id
        {
            get { return GetProperty(() => Id); }
            set { SetProperty(() => Id, value); }
        }

        public OrganizationOwnership OrganizationOwnership
        {
            get { return GetProperty(() => OrganizationOwnership); }
            set { SetProperty(() => OrganizationOwnership, value, () => { FullName = GetOrganizationFullName(Name, OrganizationOwnership); }); }
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

        public string Name
        {
            get { return GetProperty(() => Name); }
            set { SetProperty(() => Name, value, () => { FullName = GetOrganizationFullName(Name, OrganizationOwnership); }); }
        }

        public string FullName
        {
            get { return GetProperty(() => FullName); }
            set { SetProperty(() => FullName, value); }
        }

        public string LegalAddress
        {
            get { return GetProperty(() => LegalAddress); }
            set { SetProperty(() => LegalAddress, value); }
        }

        public string PostAddress
        {
            get { return GetProperty(() => PostAddress); }
            set { SetProperty(() => PostAddress, value); }
        }

        public string Inn
        {
            get { return GetProperty(() => Inn); }
            set { SetProperty(() => Inn, value); }
        }

        public string InnVatPayer
        {
            get { return GetProperty(() => InnVatPayer); }
            set { SetProperty(() => InnVatPayer, value); }
        }

        public string Okpo
        {
            get { return GetProperty(() => Okpo); }
            set { SetProperty(() => Okpo, value); }
        }

        public bool IsVatPayer
        {
            get { return GetProperty(() => IsVatPayer); }
            set { SetProperty(() => IsVatPayer, value); }
        }

        public string VatLicence
        {
            get { return GetProperty(() => VatLicence); }
            set { SetProperty(() => VatLicence, value); }
        }

        public string TaxPayerLicence
        {
            get { return GetProperty(() => TaxPayerLicence); }
            set { SetProperty(() => TaxPayerLicence, value); }
        }

        public string Comment
        {
            get { return GetProperty(() => Comment); }
            set { SetProperty(() => Comment, value); }
        }

        public bool Active
        {
            get { return GetProperty(() => Active); }
            set { SetProperty(() => Active, value); }
        }

        #region IDataErrorInfo

        string IDataErrorInfo.Error => string.Empty;

        string IDataErrorInfo.this[string columnName] => IDataErrorInfoHelper.GetErrorText(this, columnName);

        #endregion

        public static void BuildMetadata(MetadataBuilder<OrganizationViewItem> builder)
        {
            builder.Property(x => x.Name)
                .Required(() => Resources.RequiredErrorMessage)
                .MaxLength(OrganizationConstants.NameMaxLength);

            builder.Property(x => x.LegalAddress)
                .Required(() => Resources.RequiredErrorMessage);

            builder.Property(x => x.PostAddress)
                .Required(() => Resources.RequiredErrorMessage);

            builder.Property(x => x.Inn)
                .MatchesInstanceRule(
                    (x, y) => y.OrganizationOwnership.IsLegal || (x != null && x.Length == OrganizationConstants.InnMaxLength),
                    () => "Неверная длина ИНН");

            builder.Property(x => x.InnVatPayer)
                .MatchesInstanceRule(
                    (x, y) => !y.OrganizationOwnership.IsLegal || (x != null && x.Length == OrganizationConstants.InnVatPayerMaxLength),
                    () => "Неверная длина ИНН НДС");

            builder.Property(x => x.Okpo)
                .MatchesInstanceRule((x, y) => !y.OrganizationOwnership.IsLegal || !string.IsNullOrEmpty(x), () => Resources.RequiredErrorMessage);

            builder.Property(x => x.TaxPayerLicence)
                .MaxLength(OrganizationConstants.TaxPayerLicenceMaxLength);

            builder.Property(x => x.Comment)
                .MaxLength(OrganizationConstants.CommentMaxLength);
        }

        object ICloneable.Clone()
        {
            return Clone();
        }

        public OrganizationViewItem Clone()
        {
            return ReflectionObjectCloner.Clone(this);
        }

        private static string GetOrganizationFullName(string name, OrganizationOwnership ownership)
        {
            if (ownership == null)
            {
                return name;
            }

            string organizationName = ownership.IsLegal ? $"\"{name}\"" : name;
            return $"{ownership.Description} {organizationName}";
        }
    }
}
