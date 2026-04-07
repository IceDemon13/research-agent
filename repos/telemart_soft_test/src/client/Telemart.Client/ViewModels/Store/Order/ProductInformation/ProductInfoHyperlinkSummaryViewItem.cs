using DevExpress.Mvvm;

namespace Telemart.Client.ViewModels.Store.Order.ProductInformation
{
    public class ProductInfoHyperlinkSummaryViewItem : BindableBase, ILinkSupport
    {
        public ProductInfoHyperlinkSummaryViewItem(string title, string name, string link = null)
        {
            Title = title;
            ContractorName = name;
            Link = link;
        }

        public string Title
        {
            get { return GetProperty(() => Title); }
            set { SetProperty(() => Title, value); }
        }

        public string ContractorName
        {
            get { return GetProperty(() => ContractorName); }
            set { SetProperty(() => ContractorName, value); }
        }

        public string Link
        {
            get { return GetProperty(() => Link); }
            set { SetProperty(() => Link, value); }
        }
    }
}
