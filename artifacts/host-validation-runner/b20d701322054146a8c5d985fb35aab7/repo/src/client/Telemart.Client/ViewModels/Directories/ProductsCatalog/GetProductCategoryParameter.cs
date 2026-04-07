namespace Telemart.Client.ViewModels.Directories.ProductsCatalog
{
    public class GetProductCategoryParameter
    {
        public GetProductCategoryParameter(int? rootCategoryId, int? selectedCategoryId)
        {
            RootCategoryId = rootCategoryId;
            SelectedCategoryId = selectedCategoryId;
        }

        public int? RootCategoryId { get; }

        public int? SelectedCategoryId { get; }
    }
}
