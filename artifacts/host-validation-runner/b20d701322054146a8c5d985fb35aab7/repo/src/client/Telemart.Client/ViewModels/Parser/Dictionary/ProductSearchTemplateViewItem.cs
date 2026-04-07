using System.Collections.ObjectModel;
using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.Parser.Dictionary
{
    public class ProductSearchTemplateViewItem : TelemartCloneableViewItemBase
    {
        public int ContractorId
        {
            get { return GetProperty(() => ContractorId); }
            set { SetProperty(() => ContractorId, value); }
        }

        public int ParserSearchTemplateId
        {
            get { return GetProperty(() => ParserSearchTemplateId); }
            set { SetProperty(() => ParserSearchTemplateId, value); }
        }

        public long ParserAliasId
        {
            get { return GetProperty(() => ParserAliasId); }
            set { SetProperty(() => ParserAliasId, value); }
        }

        public string Link
        {
            get { return GetProperty(() => Link); }
            set { SetProperty(() => Link, value, () => RaisePropertyChanged(nameof(LinkEnabled))); }
        }

        public string ProductName
        {
            get { return GetProperty(() => ProductName); }
            set { SetProperty(() => ProductName, value); }
        }

        public int ProductId
        {
            get { return GetProperty(() => ProductId); }
            set { SetProperty(() => ProductId, value); }
        }

        public string ContractorProductName
        {
            get { return GetProperty(() => ContractorProductName); }
            set { SetProperty(() => ContractorProductName, value); }
        }

        public bool Active
        {
            get { return GetProperty(() => Active); }
            set { SetProperty(() => Active, value); }
        }

        public ObservableCollection<ProductFeatureValueSearchTemplateViewItem> Features
        {
            get { return GetProperty(() => Features); }
            private set { SetProperty(() => Features, value); }
        }

        public bool LinkEnabled => !string.IsNullOrWhiteSpace(Link) && Link.Contains("http");
    }
}