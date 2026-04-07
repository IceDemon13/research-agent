using DevExpress.Mvvm;

namespace Telemart.Client.ViewModels.Store.Order.ProductInformation
{
    public class HotlineCompetitorViewItem : BindableBase
    {
        public int Id
        {
            get { return GetProperty(() => Id); }
            set { SetProperty(() => Id, value); }
        }

        public int AbcId
        {
            get { return GetProperty(() => AbcId); }
            set { SetProperty(() => AbcId, value); }
        }

        public int? ContractorId
        {
            get { return GetProperty(() => ContractorId); }
            set { SetProperty(() => ContractorId, value); }
        }

        public string Name
        {
            get { return GetProperty(() => Name); }
            set { SetProperty(() => Name, value); }
        }

        public double? Rating
        {
            get { return GetProperty(() => Rating); }
            set { SetProperty(() => Rating, value); }
        }

        public int CommentCount
        {
            get { return GetProperty(() => CommentCount); }
            set { SetProperty(() => CommentCount, value); }
        }
    }
}
