using System.Collections.ObjectModel;
using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.AssembledComputerRule
{
    public class AssemblySlotHostConsumerItem : TelemartViewItemBase
    {
        public AssemblySlotHostConsumerItem()
        {
        }

        public int Id
        {
            get { return GetProperty(() => Id); }
            set { SetProperty(() => Id, value); }
        }

        public string ValidationMessageTemplate
        {
            get { return GetProperty(() => ValidationMessageTemplate); }
            set { SetProperty(() => ValidationMessageTemplate, value); }
        }

        public int CategoryId
        {
            get { return GetProperty(() => CategoryId); }
            set { SetProperty(() => CategoryId, value); }
        }

        public int? FeatureId
        {
            get { return GetProperty(() => FeatureId); }
            set { SetProperty(() => FeatureId, value); }
        }

        public int ConsumerCategoryId
        {
            get { return GetProperty(() => CategoryId); }
            set { SetProperty(() => CategoryId, value); }
        }

        public int? ConsumerFeatureId
        {
            get { return GetProperty(() => FeatureId); }
            set { SetProperty(() => FeatureId, value); }
        }

        public bool Ignore
        {
            get { return GetProperty(() => Ignore); }
            set { SetProperty(() => Ignore, value); }
        }
    }
}