using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.Content.ProductFeatureGroups
{
    public class FeatureContractorKeyViewItem : TelemartViewItemBase
    {
        public FeatureContractorKeyViewItem(int id, int contractorId, string contractorName, string key)
        {
            Id = id;
            ContractorId = contractorId;
            ContractorName = contractorName;
            Key = key;
        }

        public FeatureContractorKeyViewItem()
        {
        }

        public int Id
        {
            get { return GetProperty(() => Id); }
            set { SetProperty(() => Id, value); }
        }

        public int ContractorId
        {
            get { return GetProperty(() => ContractorId); }
            set { SetProperty(() => ContractorId, value); }
        }

        public string ContractorName
        {
            get { return GetProperty(() => ContractorName); }
            set { SetProperty(() => ContractorName, value); }
        }

        public string Key
        {
            get { return GetProperty(() => Key); }
            set { SetProperty(() => Key, value); }
        }
    }
}