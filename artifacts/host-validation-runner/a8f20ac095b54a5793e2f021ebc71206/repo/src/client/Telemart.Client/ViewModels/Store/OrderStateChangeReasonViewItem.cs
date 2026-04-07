using DevExpress.Mvvm;

namespace Telemart.Client.ViewModels.Store
{
    public sealed class OrderStateChangeReasonViewItem : BindableBase
    {
        public OrderStateChangeReasonViewItem(int id, int parentId, string name, int position)
        {
            Id = id;
            ParentId = parentId;
            Name = name;
            Position = position;
        }

        public int Id
        {
            get { return GetProperty(() => Id); }
            set { SetProperty(() => Id, value); }
        }

        public int ParentId
        {
            get { return GetProperty(() => ParentId); }
            set { SetProperty(() => ParentId, value, () => { RaisePropertyChanged(nameof(IsParent)); }); }
        }

        public string Name
        {
            get { return GetProperty(() => Name); }
            set { SetProperty(() => Name, value); }
        }

        public int Position
        {
            get { return GetProperty(() => Position); }
            set { SetProperty(() => Position, value); }
        }

        public bool IsParent => ParentId <= 0;
    }
}