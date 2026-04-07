using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.Store.Call
{
    public class CallDependencyViewItem : TelemartViewItemBase
    {
        public int Id
        {
            get { return GetProperty(() => Id); }
            set { SetProperty(() => Id, value); }
        }

        public int? DependencyTypeId
        {
            get { return GetProperty(() => DependencyTypeId); }
            set { SetProperty(() => DependencyTypeId, value); }
        }

        public int? DocumentId
        {
            get { return GetProperty(() => DocumentId); }
            set { SetProperty(() => DocumentId, value); }
        }

        public string Name
        {
            get { return GetProperty(() => Name); }
            set { SetProperty(() => Name, value); }
        }

        public string Comment
        {
            get { return GetProperty(() => Comment); }
            set { SetProperty(() => Comment, value); }
        }

        public bool Approved
        {
            get { return GetProperty(() => Approved); }
            set { SetProperty(() => Approved, value); }
        }

        public bool System
        {
            get { return GetProperty(() => System); }
            set { SetProperty(() => System, value); }
        }
    }
}