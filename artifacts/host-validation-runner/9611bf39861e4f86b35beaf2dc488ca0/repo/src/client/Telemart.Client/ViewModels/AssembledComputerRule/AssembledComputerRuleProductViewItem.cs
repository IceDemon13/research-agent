using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.AssembledComputerRule
{
    public class AssembledComputerRuleProductViewItem : TelemartViewItemBase
    {
        public int Id
        {
            get { return GetProperty(() => Id); }
            set { SetProperty(() => Id, value); }
        }

        public int ProductId
        {
            get { return GetProperty(() => ProductId); }
            set { SetProperty(() => ProductId, value); }
        }

        public int CategoryId
        {
            get { return GetProperty(() => CategoryId); }
            set { SetProperty(() => CategoryId, value); }
        }

        public int ParentCategoryId
        {
            get { return GetProperty(() => ParentCategoryId); }
            set { SetProperty(() => ParentCategoryId, value); }
        }

        public string ProductName
        {
            get { return GetProperty(() => ProductName); }
            set { SetProperty(() => ProductName, value); }
        }

        public int QuantityFree
        {
            get { return GetProperty(() => QuantityFree); }
            set { SetProperty(() => QuantityFree, value); }
        }

        public decimal Price
        {
            get { return GetProperty(() => Price); }
            set { SetProperty(() => Price, value); }
        }

        public bool Required
        {
            get
            {
                return GetProperty(() => Required);
            }

            set
            {
                if (SetProperty(() => Required, value))
                {
                    QuantityToUse = !value ? null : 1;
                }
            }
        }

        public int? QuantityToUse
        {
            get
            {
                return GetProperty(() => QuantityToUse);
            }

            set
            {
                if (SetProperty(() => QuantityToUse, value))
                {
                    Required = value > 0 ? true : false;
                }
            }
        }

        public int Priority
        {
            get { return GetProperty(() => Priority); }
            set { SetProperty(() => Priority, value); }
        }
    }
}
