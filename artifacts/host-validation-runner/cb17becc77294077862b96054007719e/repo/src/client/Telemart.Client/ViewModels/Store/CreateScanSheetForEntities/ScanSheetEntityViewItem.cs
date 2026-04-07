using DevExpress.Mvvm;

namespace Telemart.Client.ViewModels.Store.CreateScanSheetForEntities
{
    public class ScanSheetEntityViewItem : BindableBase
    {
        public int Id
        {
            get { return GetProperty(() => Id); }
            set { SetProperty(() => Id, value); }
        }

        public string TtnId
        {
            get { return GetProperty(() => TtnId); }
            set { SetProperty(() => TtnId, value); }
        }

        public bool Selected
        {
            get { return GetProperty(() => Selected); }
            set { SetProperty(() => Selected, value); }
        }
    }
}
