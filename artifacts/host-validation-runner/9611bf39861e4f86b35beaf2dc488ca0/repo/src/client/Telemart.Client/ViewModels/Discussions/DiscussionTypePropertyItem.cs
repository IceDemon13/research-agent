using DevExpress.Mvvm.DataAnnotations;
using Telemart.Client.ViewModels.Base;
using Telemart.Client.Properties;

namespace Telemart.Client.ViewModels.Discussions
{
    public sealed class DiscussionTypePropertyItem : TelemartEditorViewItemBase
    {
        public DiscussionTypePropertyItem(int id, string name, string displayName, bool required, int position, string discussionText)
        {
            Id = id;
            NameProperty = name;
            DisplayNameProperty = displayName;
            Required = required;
            Position = position;
            DiscussionText = discussionText;
        }

        public DiscussionTypePropertyItem()
        {
        }

        public string NameProperty
        {
            get { return GetProperty(() => NameProperty); }
            set { SetProperty(() => NameProperty, value); }
        }

        public string DisplayNameProperty
        {
            get { return GetProperty(() => DisplayNameProperty); }
            set { SetProperty(() => DisplayNameProperty, value); }
        }

        public string DiscussionText
        {
            get { return GetProperty(() => DiscussionText); }
            set { SetProperty(() => DiscussionText, value, () => RaisePropertyChanged(nameof(VisibleImage))); }
        }

        public bool Required
        {
            get { return GetProperty(() => Required); }
            set { SetProperty(() => Required, value, () => RaisePropertyChanged(nameof(VisibleImage))); }
        }

        public int Position
        {
            get { return GetProperty(() => Position); }
            set { SetProperty(() => Position, value); }
        }

        public bool VisibleImage => Required && string.IsNullOrEmpty(DiscussionText);

        public static void BuildMetadata(MetadataBuilder<DiscussionTypePropertyItem> builder)
        {
            builder.Property(x => x.DiscussionText)
                .MatchesInstanceRule((x, y) => !y.Required || !string.IsNullOrEmpty(x), () => Resources.RequiredErrorMessage);
        }
    }
}