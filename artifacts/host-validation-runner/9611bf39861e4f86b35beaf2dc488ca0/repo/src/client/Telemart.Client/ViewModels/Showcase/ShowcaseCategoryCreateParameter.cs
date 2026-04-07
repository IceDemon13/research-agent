namespace Telemart.Client.ViewModels.Showcase
{
    public sealed class ShowcaseCategoryCreateParameter
    {
        public ShowcaseCategoryCreateParameter(string[] placeNames, string title)
        {
            PlaceNames = placeNames;
            Title = title;
        }

        public ShowcaseCategoryCreateParameter(string[] placeNames, string title, ShowcaseCategoryViewItem selectedShowcaseCategory)
        {
            SelectedShowcaseCategory = selectedShowcaseCategory;
            PlaceNames = placeNames;
            Title = title;
        }

        public string[] PlaceNames { get; }

        public string Title { get; }

        public ShowcaseCategoryViewItem SelectedShowcaseCategory { get; }
    }
}