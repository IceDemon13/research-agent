namespace Telemart.Client.ViewModels.Directories.Category
{
    public class CategoryEditMaskParameter
    {
        public CategoryEditMaskParameter(int categoryId, int languageId)
        {
            CategoryId = categoryId;
            LanguageId = languageId;
        }

        public int CategoryId { get; }

        public int LanguageId { get; }
    }
}