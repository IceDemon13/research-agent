using System.Collections.ObjectModel;
using Telemart.Client.Common.MvvmEnhancements;
using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.Content.ProductFeatureGroups
{
    public class FeatureContractorParserSourceViewItem : TelemartCloneableViewItemBase
    {
        public int Id
        {
            get { return GetProperty(() => Id); }
            set { SetProperty(() => Id, value); }
        }

        public int FeatureId
        {
            get { return GetProperty(() => FeatureId); }
            set { SetProperty(() => FeatureId, value); }
        }

        public string FeatureName
        {
            get { return GetProperty(() => FeatureName); }
            set { SetProperty(() => FeatureName, value); }
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

        public int Priority
        {
            get { return GetProperty(() => Priority); }
            set { SetProperty(() => Priority, value); }
        }

        public ReadOnlyObservableCollection<ComboBoxItem> AllowedPriorities
        {
            get { return GetProperty(() => AllowedPriorities); }
            set { SetProperty(() => AllowedPriorities, value); }
        }
    }
}