using System.Collections.Generic;

namespace Telemart.Client.ViewModels.Store.Order
{
    public class BonusConfirmationParameter
    {
        public BonusConfirmationParameter(int customerBonuses, IEnumerable<BonusConfirmationViewItem> items)
        {
            CustomerBonuses = customerBonuses;
            Items = items;
        }

        public int CustomerBonuses { get; }

        public IEnumerable<BonusConfirmationViewItem> Items { get; }
    }
}
