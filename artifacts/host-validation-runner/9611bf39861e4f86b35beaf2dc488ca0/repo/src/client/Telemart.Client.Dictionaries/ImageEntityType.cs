namespace Telemart.Client.Dictionaries
{
    public sealed class ImageEntityType : DictionaryItemBase
    {
        private const int CommentIndex = 1;
        private const int FeatureIndex = 2;

        private ImageEntityType(int id, string name)
            : base(id, name)
        {
        }

        public static ImageEntityType Comment { get; } = new ImageEntityType(CommentIndex, "comment");

        public static ImageEntityType Feature { get; } = new ImageEntityType(FeatureIndex, "feature");
    }
}