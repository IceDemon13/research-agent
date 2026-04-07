namespace Telemart.Client.ViewModels.Dialogs
{
    public class GetFeatureFromUserViewModelParameter
    {
        public GetFeatureFromUserViewModelParameter(string title)
        {
            Title = title;
        }

        public GetFeatureFromUserViewModelParameter(string title, int categoryId)
            : this(title)
        {
            CategotyId = categoryId;
        }

        public string Title { get; }

        public int? CategotyId { get; }
    }
}