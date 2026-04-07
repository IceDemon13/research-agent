using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.AssemblyTest
{
    public class AssemblyTestSlaveCategoryViewItem : TelemartEditorViewItemBase
    {
        public AssemblyTestSlaveCategoryViewItem(int categoryId, int featureId)
        {
            CategoryId = categoryId;
            FeatureId = featureId;
        }

        public AssemblyTestSlaveCategoryViewItem()
        {
        }

        public int CategoryId
        {
            get { return GetProperty(() => CategoryId); }
            set { SetProperty(() => CategoryId, value); }
        }

        public int FeatureId
        {
            get { return GetProperty(() => FeatureId); }
            set { SetProperty(() => FeatureId, value); }
        }
    }
}