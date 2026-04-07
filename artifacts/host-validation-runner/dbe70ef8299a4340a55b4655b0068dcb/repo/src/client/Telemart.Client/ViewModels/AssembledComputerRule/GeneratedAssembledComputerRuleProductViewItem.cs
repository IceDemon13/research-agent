using Telemart.Client.Dictionaries;
using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.AssembledComputerRule
{
    public class GeneratedAssembledComputerRuleProductViewItem : TelemartViewItemBase
    {
        public int ProductId
        {
            get { return GetProperty(() => ProductId); }
            set { SetProperty(() => ProductId, value); }
        }

        public string ProductName
        {
            get { return GetProperty(() => ProductName); }
            set { SetProperty(() => ProductName, value); }
        }

        public int Quantity
        {
            get { return GetProperty(() => Quantity); }
            set { SetProperty(() => Quantity, value); }
        }

        public decimal? PriceUah
        {
            get { return GetProperty(() => PriceUah); }
            set { SetProperty(() => PriceUah, value); }
        }
    }
}