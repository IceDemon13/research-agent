using DevExpress.Mvvm;

namespace Telemart.Client.ViewModels.TradeIn
{
    public sealed class TradeInReasonCancelTypeViewItem : BindableBase
    {
        public int Id
        {
            get { return GetProperty(() => Id); }
            set { SetProperty(() => Id, value); }
        }

        public int? ParentId
        {
            get { return GetProperty(() => ParentId); }
            set { SetProperty(() => ParentId, value, () => { RaisePropertyChanged(nameof(IsParent)); }); }
        }

        public string Name
        {
            get { return GetProperty(() => Name); }
            set { SetProperty(() => Name, value); }
        }

        public bool IsParent => !ParentId.HasValue || ParentId <= 0;
    }
}