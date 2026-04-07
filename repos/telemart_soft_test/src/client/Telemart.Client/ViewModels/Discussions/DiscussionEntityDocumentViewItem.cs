using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.Discussions
{
    public class DiscussionEntityDocumentViewItem : TelemartViewItemBase
    {
        public int Id
        {
            get { return GetProperty(() => Id); }
            set { SetProperty(() => Id, value); }
        }

        public int DiscussionId
        {
            get { return GetProperty(() => DiscussionId); }
            set { SetProperty(() => DiscussionId, value); }
        }

        public int EntityId
        {
            get { return GetProperty(() => EntityId); }
            set { SetProperty(() => EntityId, value); }
        }

        public int DocumentId
        {
            get { return GetProperty(() => DocumentId); }
            set { SetProperty(() => DocumentId, value); }
        }
    }
}