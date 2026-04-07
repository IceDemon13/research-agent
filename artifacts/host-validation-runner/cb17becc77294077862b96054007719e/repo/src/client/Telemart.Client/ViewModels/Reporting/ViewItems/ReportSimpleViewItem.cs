using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.Reporting.ViewItems
{
    public class ReportSimpleViewItem : TelemartViewItemBase
    {
        public int Id
        {
            get { return GetProperty(() => Id); }
            init { SetProperty(() => Id, value); }
        }

        public string Name
        {
            get { return GetProperty(() => Name); }
            init { SetProperty(() => Name, value); }
        }

        public string Description
        {
            get { return GetProperty(() => Description); }
            init { SetProperty(() => Description, value); }
        }

        public bool IsFolder
        {
            get { return GetProperty(() => IsFolder); }
            init { SetProperty(() => IsFolder, value); }
        }

        public int? ParentId
        {
            get { return GetProperty(() => ParentId); }
            init { SetProperty(() => ParentId, value); }
        }
    }
}