using System.Collections.ObjectModel;
using Telemart.Client.Dictionaries;

namespace Telemart.Client.ViewModels.Customer
{
    public class CustomerTransferBonusesParameter
    {
        public CustomerTransferBonusesParameter(int senderCustomerId, BonusType selectedBonusType, ReadOnlyObservableCollection<BonusType> bonusTypes)
        {
            SenderCustomerId = senderCustomerId;
            SelectedBonusType = selectedBonusType;
            BonusTypes = bonusTypes;
        }

        public int SenderCustomerId { get; set; }

        public BonusType SelectedBonusType { get; set; }

        public ReadOnlyObservableCollection<BonusType> BonusTypes { get; set; }
    }
}
