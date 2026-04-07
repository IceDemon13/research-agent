using System;
using System.Collections.ObjectModel;
using System.Linq;
using DevExpress.Mvvm.DataAnnotations;
using DevExpress.Mvvm.POCO;
using Telemart.Client.Common;
using Telemart.Client.Common.MvvmEnhancements;
using Telemart.Client.Common.Validation;
using Telemart.Client.Core.Cloning;
using Telemart.Client.Properties;

namespace Telemart.Client.ViewModels.Directories.Contractor
{
    [POCOViewModel(ImplementIDataErrorInfo = true)]
    public class ContractorContactViewItem : ICloneable
    {
        protected ContractorContactViewItem()
        {
        }

        public virtual int Id { get; set; }

        public virtual bool Active { get; set; }

        public virtual int? CityId { get; set; }

        public virtual int? CountryId { get; set; }

        public virtual string Name { get; set; }

        public virtual string NameEn { get; set; }

        public virtual string Surname { get; set; }

        public virtual string SurnameEn { get; set; }

        public virtual string Patronymic { get; set; }

        public virtual string Phone1 { get; set; }

        public virtual string Phone2 { get; set; }

        public virtual string Email { get; set; }

        public virtual string Skype { get; set; }

        public virtual string Comment { get; set; }

        public virtual string CityName { get; protected set; }

        public virtual ObservableCollection<ComboBoxItem> Positions { get; set; }

        public string FullName => $"{Surname} {Name} {Patronymic}";

        public string PositionsText => string.Join(", ", Positions.Select(x => x.DisplayValue));

        public static void BuildMetadata(MetadataBuilder<ContractorContactViewItem> builder)
        {
            builder.Property(x => x.Name).ApplyFioValidationRules(() => "Введите имя (кириллица, макс 100 символов)");
            builder.Property(x => x.NameEn).ApplyFioLatinValidationRules(() => "Введите имя (латиница, макс 100 символов)");
            builder.Property(x => x.Surname).ApplyFioValidationRules(() => "Введите фамилию (кириллица, макс 100 символов)");
            builder.Property(x => x.SurnameEn).ApplyFioLatinValidationRules(() => "Введите фамилию (латиница, макс 100 символов)");
            builder.Property(x => x.Patronymic).ApplyCyrillicValidationRules(() => "Введите отчество (кириллица, макс 100 символов)");
            builder.Property(x => x.Phone1).Required(() => Resources.RequiredErrorMessage);
            builder.Property(x => x.Email)
                .EmailAddressDataType(() => "Введите корректный E-mail")
                .MaxLength(ContractorConstants.EmailMaxLength);
            builder.Property(x => x.Positions)
                .MatchesInstanceRule((x, y) => x != null && x.Count > 0, () => Resources.RequiredErrorMessage);
            builder.Property(x => x.CityId).Required(() => Resources.RequiredErrorMessage);
            builder.Property(x => x.Comment).MaxLength(ContractorConstants.CommentMaxLength);
            builder.Property(x => x.Skype).MaxLength(ContractorConstants.SkypeMaxLength);
        }

        public static ContractorContactViewItem Create()
        {
            ContractorContactViewItem item = ViewModelSource<ContractorContactViewItem>.Create();
            item.Active = true;
            item.Positions = new ObservableCollection<ComboBoxItem>();
            return item;
        }

        public ContractorContactViewItem SetCityName(ReadOnlyObservableCollection<ContractorCityItem> citiesOfCountry)
        {
            if (CityId.HasValue && citiesOfCountry != null)
            {
                int countryId = CountryId ?? Constants.UkraineCountryId;

                CityName = citiesOfCountry.FirstOrDefault(x => x.CountryId == countryId && x.Id == CityId)?.Name;
            }

            return this;
        }

        object ICloneable.Clone()
        {
            return Clone();
        }

        public ContractorContactViewItem Clone()
        {
            return ReflectionObjectCloner.Clone(this, Create);
        }

        protected void OnNameChanged(string oldValue)
        {
            this.RaisePropertyChanged(x => x.FullName);
        }

        protected void OnSurnameChanged(string oldValue)
        {
            this.RaisePropertyChanged(x => x.FullName);
        }

        protected void OnPatronymicChanged(string oldValue)
        {
            this.RaisePropertyChanged(x => x.FullName);
        }

        protected void OnPositionsChanged(ObservableCollection<ComboBoxItem> oldValue)
        {
            this.RaisePropertyChanged(x => x.PositionsText);
        }
    }
}
