using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.AdditionalService
{
    public class AdditionalServiceSlaveCategoryViewItem : TelemartViewItemBase
    {
        public int Id
        {
            get { return GetProperty(() => Id); }
            set { SetProperty(() => Id, value); }
        }

        public int? CategoryId
        {
            get { return GetProperty(() => CategoryId); }
            set { SetProperty(() => CategoryId, value); }
        }

        public int? ProductId
        {
            get { return GetProperty(() => ProductId); }
            set { SetProperty(() => ProductId, value); }
        }

        public string CategoryName
        {
            get { return GetProperty(() => CategoryName); }
            set { SetProperty(() => CategoryName, value); }
        }

        public string Feature
        {
            get { return GetProperty(() => Feature); }
            set { SetProperty(() => Feature, value); }
        }

        public int? FeatureId
        {
            get { return GetProperty(() => FeatureId); }
            set { SetProperty(() => FeatureId, value); }
        }

        public string FeatureValue
        {
            get { return GetProperty(() => FeatureValue); }
            set { SetProperty(() => FeatureValue, value); }
        }

        public int? FeatureValueId
        {
            get { return GetProperty(() => FeatureValueId); }
            set { SetProperty(() => FeatureValueId, value); }
        }

        public string ProductName
        {
            get { return GetProperty(() => ProductName); }
            set { SetProperty(() => ProductName, value); }
        }

        public bool IsCategory => CategoryId.HasValue;
    }
}