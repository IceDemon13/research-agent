using System;
using System.Collections.ObjectModel;
using DevExpress.Mvvm.DataAnnotations;
using Telemart.Client.Common;
using Telemart.Client.Properties;
using Telemart.Client.ViewModels.Base;
using Telemart.Common.Constants;

namespace Telemart.Client.ViewModels.Customer
{
    public class CustomerViewItem : TelemartEditorViewItemBase
    {
        public int? EmployeeId
        {
            get { return GetProperty(() => EmployeeId); }
            set { SetProperty(() => EmployeeId, value); }
        }

        public int? CityId
        {
            get { return GetProperty(() => CityId); }
            set { SetProperty(() => CityId, value); }
        }

        public int? ContractorId
        {
            get { return GetProperty(() => ContractorId); }
            set { SetProperty(() => ContractorId, value); }
        }

        public ObservableCollection<int> PlusHashtagIds
        {
            get { return GetProperty(() => PlusHashtagIds); }
            set { SetProperty(() => PlusHashtagIds, value); }
        }

        public ObservableCollection<int> MinusHashtagIds
        {
            get { return GetProperty(() => MinusHashtagIds); }
            set { SetProperty(() => MinusHashtagIds, value); }
        }

        public ObservableCollection<CustomerBonusViewItem> Bonuses
        {
            get { return GetProperty(() => Bonuses); }
            set { SetProperty(() => Bonuses, value); }
        }

        public ObservableCollection<CustomerAssemblyViewItem> Assemblies
        {
            get { return GetProperty(() => Assemblies); }
            set { SetProperty(() => Assemblies, value); }
        }

        public string Fio
        {
            get { return GetProperty(() => Fio); }
            set { SetProperty(() => Fio, value); }
        }

        public DateTime? Birthday
        {
            get { return GetProperty(() => Birthday); }
            set { SetProperty(() => Birthday, value); }
        }

        public string Email
        {
            get { return GetProperty(() => Email); }
            set { SetProperty(() => Email, value); }
        }

        public string Phone1
        {
            get { return GetProperty(() => Phone1); }
            set { SetProperty(() => Phone1, value); }
        }

        public string Phone2
        {
            get { return GetProperty(() => Phone2); }
            set { SetProperty(() => Phone2, value); }
        }

        public DateTime ModifiedOn
        {
            get { return GetProperty(() => ModifiedOn); }
            set { SetProperty(() => ModifiedOn, value); }
        }

        public int ModifiedBy
        {
            get { return GetProperty(() => ModifiedBy); }
            set { SetProperty(() => ModifiedBy, value); }
        }

        public DateTime CreatedOn
        {
            get { return GetProperty(() => CreatedOn); }
            set { SetProperty(() => CreatedOn, value); }
        }

        public int CreatedBy
        {
            get { return GetProperty(() => CreatedBy); }
            set { SetProperty(() => CreatedBy, value); }
        }

        public bool Validated
        {
            get { return GetProperty(() => Validated); }
            set { SetProperty(() => Validated, value); }
        }

        public int ValidationTries
        {
            get { return GetProperty(() => ValidationTries); }
            set { SetProperty(() => ValidationTries, value); }
        }

        public bool NotCountBonuses
        {
            get { return GetProperty(() => NotCountBonuses); }
            set { SetProperty(() => NotCountBonuses, value); }
        }

        public static void BuildMetadata(MetadataBuilder<CustomerViewItem> builder)
        {
            builder.Property(x => x.ContractorId).Required(() => Resources.RequiredErrorMessage);

            builder.Property(x => x.Email)
                .MatchesRegularExpression(RegexConstants.EmailRegex, () => Resources.OrderViewModel_Email);

            builder.Property(x => x.ValidationTries)
                .MatchesRule(x => x is >= 0 and <= 10, () => "Значение должно быть в диапазоне 0..10");
        }
    }
}