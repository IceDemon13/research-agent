using System;
using System.Collections.ObjectModel;
using System.Linq;
using DevExpress.Mvvm.DataAnnotations;
using DevExpress.Mvvm.Native;
using Telemart.Client.Core.Cloning;
using Telemart.Client.Helpers;
using Telemart.Client.Properties;
using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.TradeInSegment
{
    public class TradeInSegmentViewItem : TelemartEditorViewItemBase
    {
        public int? CategoryId
        {
            get { return GetProperty(() => CategoryId); }
            set { SetProperty(() => CategoryId, value); }
        }

        public string CategoryName
        {
            get { return GetProperty(() => CategoryName); }
            set { SetProperty(() => CategoryName, value); }
        }

        public int? EmployeeId
        {
            get { return GetProperty(() => EmployeeId); }
            set { SetProperty(() => EmployeeId, value); }
        }

        public string EmployeeName
        {
            get { return GetProperty(() => EmployeeName); }
            set { SetProperty(() => EmployeeName, value); }
        }

        public DateTime CreatedOn
        {
            get { return GetProperty(() => CreatedOn); }
            set { SetProperty(() => CreatedOn, value); }
        }

        public DateTime ModifiedOn
        {
            get { return GetProperty(() => ModifiedOn); }
            set { SetProperty(() => ModifiedOn, value); }
        }

        public string CreatedByName
        {
            get { return GetProperty(() => CreatedByName); }
            set { SetProperty(() => CreatedByName, value); }
        }

        public string ModifiedByName
        {
            get { return GetProperty(() => ModifiedByName); }
            set { SetProperty(() => ModifiedByName, value); }
        }

        public string Name
        {
            get { return GetProperty(() => Name); }
            set { SetProperty(() => Name, value); }
        }

        public decimal? Price
        {
            get { return GetProperty(() => Price); }
            set { SetProperty(() => Price, value); }
        }

        public bool Default
        {
            get { return GetProperty(() => Default); }
            set { SetProperty(() => Default, value); }
        }

        public string FeatureString
        {
            get => CalculateFeaturesString();
            set { SetProperty(() => FeatureString, value); }
        }

        public bool AnyNewFeatures
        {
            get { return GetProperty(() => AnyNewFeatures); }
            set { SetProperty(() => AnyNewFeatures, value); }
        }

        public ObservableCollection<TradeInSegmentCategoryFeatureViewItem> CategoryFeatures
        {
            get { return GetProperty(() => CategoryFeatures); }
            set { SetProperty(() => CategoryFeatures, value, () => RaisePropertyChanged(nameof(FeatureString))); }
        }

        public static void BuildMetadata(MetadataBuilder<TradeInSegmentViewItem> builder)
        {
            builder.Property(x => x.CategoryId).Required(() => Resources.RequiredErrorMessage);
            builder.Property(x => x.Name).Required(() => Resources.RequiredErrorMessage);
            builder.Property(x => x.EmployeeId).Required(() => Resources.RequiredErrorMessage);
            builder.Property(x => x.Price).MatchesRule(x => x is null or > 0 and < 1_000_000, () => "Значение должно быть больше 0 и меньше 1 000 000");
        }

        public override object Clone()
        {
            TradeInSegmentViewItem item = ReflectionObjectCloner.Clone(this);

            item.CategoryFeatures = CategoryFeatures?.Select(x =>
            {
                object viewItem = x.Clone();

                return (TradeInSegmentCategoryFeatureViewItem)viewItem;
            }).ToObservableCollection();

            return item;
        }

        private string CalculateFeaturesString()
        {
            if (CategoryFeatures?.Any() != true)
            {
                return null;
            }

            return CategoryFeatures.GetSegmentFeaturesString();
        }
    }
}