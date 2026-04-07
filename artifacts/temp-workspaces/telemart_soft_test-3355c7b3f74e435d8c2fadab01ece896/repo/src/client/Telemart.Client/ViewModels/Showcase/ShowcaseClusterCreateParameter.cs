namespace Telemart.Client.ViewModels.Showcase
{
    public class ShowcaseClusterCreateParameter
    {
        public ShowcaseClusterCreateParameter(string title, int? categoryId, ShowcaseClusterViewItem item)
        {
            Title = title;
            CategoryId = categoryId;
            SelectedShowcaseCluster = item;
        }

        public string Title { get; }

        public int? CategoryId { get; }

        public ShowcaseClusterViewItem SelectedShowcaseCluster { get; }
    }
}