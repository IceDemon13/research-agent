using System.Collections.ObjectModel;

namespace Telemart.Client.ViewModels.RobotProperties
{
    public sealed class RobotCategoryPropertiesSaveParameter
    {
        public RobotCategoryPropertiesSaveParameter(
            int productTypeId,
            int categoryId,
            string categoryName,
            ReadOnlyObservableCollection<RobotCategoryPropertyValueSaveViewItem> values)
        {
            ProductTypeId = productTypeId;
            CategoryId = categoryId;
            CategoryName = categoryName;
            Values = values;
        }

        public int CategoryId { get; }

        public int ProductTypeId { get; }

        public string CategoryName { get; }

        public ReadOnlyObservableCollection<RobotCategoryPropertyValueSaveViewItem> Values { get; }
    }
}