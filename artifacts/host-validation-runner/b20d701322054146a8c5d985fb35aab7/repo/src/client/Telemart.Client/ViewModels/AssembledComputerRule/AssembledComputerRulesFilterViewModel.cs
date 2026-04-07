using DevExpress.Mvvm;
using Telemart.Client.Data.WebClient;

namespace Telemart.Client.ViewModels.AssembledComputerRule
{
    public class AssembledComputerRulesFilterViewModel : BindableBase, IFilteringViewModel<AssembledComputerRuleFilteringItem>
    {
        public string ProductName
        {
            get { return GetProperty(() => ProductName); }
            set { SetProperty(() => ProductName, value); }
        }

        public AssembledComputerRuleFilteringItem GetFilteringItem()
        {
            AssembledComputerRuleFilteringItem item = new AssembledComputerRuleFilteringItem
            {
                ProductName = ProductName
            };

            return item;
        }

        public void SetFilteringItem(AssembledComputerRuleFilteringItem filteringItem)
        {
            ProductName = filteringItem.ProductName;
        }

        public void ResetFilterValues()
        {
            ProductName = string.Empty;
        }
    }
}
