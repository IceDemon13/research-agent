using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Telemart.Client.Dictionaries;

namespace Telemart.Client.ViewModels.Customer
{
    public class CustomerEditBonusesParameter
    {
        public CustomerEditBonusesParameter(
            ReadOnlyObservableCollection<BonusType> bonusTypes,
            BonusType selectedBonusType,
            string title,
            bool showExpireDate,
            Func<(int bonusTypeId, int quantity, DateTime? burningDate), Task<bool>> okCommand)
        {
            BonusTypes = bonusTypes;
            SelectedBonusType = selectedBonusType;
            Title = title;
            ShowExpireDate = showExpireDate;
            OkCommand = okCommand;
        }

        public ReadOnlyObservableCollection<BonusType> BonusTypes { get; }

        public BonusType SelectedBonusType { get; }

        public string Title { get; }

        public bool ShowExpireDate { get;  }

        public Func<(int bonusTypeId, int quantity, DateTime? burningDate), Task<bool>> OkCommand { get; }
    }
}