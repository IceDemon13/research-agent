using System.Collections.Generic;
using Telemart.Client.Dictionaries;

namespace Telemart.Client.ViewModels.Store.Order
{
    public class SelectBonusTypeParameter
    {
        public SelectBonusTypeParameter(IEnumerable<BonusType> bonuses)
        {
            Bonuses = bonuses;
        }

        public IEnumerable<BonusType> Bonuses { get; }
    }
}
