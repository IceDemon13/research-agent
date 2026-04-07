using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.AssembledComputerRule
{
    public class SelectAssembledComputerRuleProductViewItem : TelemartViewItemBase
    {
        public int ProductId
        {
            get { return GetProperty(() => ProductId); }
            set { SetProperty(() => ProductId, value); }
        }

        public int AssembledComputerRuleId
        {
            get { return GetProperty(() => AssembledComputerRuleId); }
            set { SetProperty(() => AssembledComputerRuleId, value); }
        }

        public string ProductName
        {
            get { return GetProperty(() => ProductName); }
            set { SetProperty(() => ProductName, value); }
        }

        public string ProductLink
        {
            get { return GetProperty(() => ProductLink); }
            set { SetProperty(() => ProductLink, value); }
        }

        public decimal Price
        {
            get { return GetProperty(() => Price); }
            set { SetProperty(() => Price, value); }
        }

        public int Quantity
        {
            get { return GetProperty(() => Quantity); }
            set { SetProperty(() => Quantity, value); }
        }
    }
}
