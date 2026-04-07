using DevExpress.Mvvm.DataAnnotations;
using Telemart.Client.Common.MvvmEnhancements;
using Telemart.Client.Dictionaries;
using Telemart.Client.Properties;
using Telemart.Client.TransferObjects.Content;
using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.AssemblyFullRule
{
    public class AssemblyFullRuleViewItem : TelemartEditorViewItemBase
    {
        public int? MasterCategoryId
        {
            get { return GetProperty(() => MasterCategoryId); }
            set { SetProperty(() => MasterCategoryId, value); }
        }

        public int? SlaveCategoryId
        {
            get { return GetProperty(() => SlaveCategoryId); }
            set { SetProperty(() => SlaveCategoryId, value, () => RaisePropertyChanged(nameof(Operation))); }
        }

        public AssemblyFullRuleOperation Operation
        {
            get { return GetProperty(() => Operation); }
            set { SetProperty(() => Operation, value, () => RaisePropertyChanged(nameof(Feature))); }
        }

        public HierarchicalItem? Feature
        {
            get { return GetProperty(() => Feature); }
            set { SetProperty(() => Feature, value); }
        }

        public ComboBoxItem? FeatureValue
        {
            get { return GetProperty(() => FeatureValue); }
            set { SetProperty(() => FeatureValue, value); }
        }

        public bool Active
        {
            get { return GetProperty(() => Active); }
            set { SetProperty(() => Active, value); }
        }

        public static void BuildMetadata(MetadataBuilder<AssemblyFullRuleViewItem> builder)
        {
            builder.Property(x => x.MasterCategoryId)
                .Required(() => Resources.RequiredErrorMessage);

            builder.Property(x => x.Operation)
                .MatchesInstanceRule((x, y) => !y.SlaveCategoryId.HasValue || x != null, () => Resources.RequiredErrorMessage);

            builder.Property(x => x.Feature)
                .MatchesInstanceRule((x, y) => y.Operation?.CanCompareValues != true || x.HasValue, () => Resources.RequiredErrorMessage);
        }
    }
}
