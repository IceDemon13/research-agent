using DevExpress.Mvvm;
using Telemart.Client.Common;

namespace Telemart.Client.ViewModels.Backlog
{
    public class BacklogCategoryViewItem : BindableBase, ICheckableTreeItem
    {
        public int Id
        {
            get { return GetProperty(() => Id); }
            set { SetProperty(() => Id, value); }
        }

        public int? ParentId
        {
            get { return GetProperty(() => ParentId); }
            set { SetProperty(() => ParentId, value); }
        }

        public string Name
        {
            get { return GetProperty(() => Name); }
            set { SetProperty(() => Name, value); }
        }

        public int LevelDepth
        {
            get { return GetProperty(() => LevelDepth); }
            set { SetProperty(() => LevelDepth, value); }
        }

        public bool Active
        {
            get { return GetProperty(() => Active); }
            set { SetProperty(() => Active, value); }
        }

        public bool? Checked
        {
            get { return GetProperty(() => Checked); }
            set { SetProperty(() => Checked, value); }
        }

        public bool Marked
        {
            get { return GetProperty(() => Marked); }
            set { SetProperty(() => Marked, value); }
        }
    }
}
