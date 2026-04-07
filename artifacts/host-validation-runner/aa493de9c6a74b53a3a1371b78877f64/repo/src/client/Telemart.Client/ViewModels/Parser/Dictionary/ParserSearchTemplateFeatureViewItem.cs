using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.Parser.Dictionary
{
    public class ParserSearchTemplateFeatureViewItem : TelemartViewItemBase
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
    }
}