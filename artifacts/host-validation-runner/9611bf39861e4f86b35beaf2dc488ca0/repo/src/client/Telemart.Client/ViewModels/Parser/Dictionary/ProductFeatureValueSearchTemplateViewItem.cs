using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.Parser.Dictionary
{
    public class ProductFeatureValueSearchTemplateViewItem : TelemartViewItemBase
    {
        public int FeatureId
        {
            get { return GetProperty(() => FeatureId); }
            private set { SetProperty(() => FeatureId, value); }
        }

        public string FeatureName
        {
            get { return GetProperty(() => FeatureName); }
            private set { SetProperty(() => FeatureName, value); }
        }

        public string FeatureValue
        {
            get { return GetProperty(() => FeatureValue); }
            private set { SetProperty(() => FeatureValue, value); }
        }

        public string ContractorFeatureValue
        {
            get { return GetProperty(() => ContractorFeatureValue); }
            private set { SetProperty(() => ContractorFeatureValue, value); }
        }
    }
}