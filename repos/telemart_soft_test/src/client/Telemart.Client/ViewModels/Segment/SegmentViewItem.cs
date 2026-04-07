using System;
using System.Collections.ObjectModel;
using System.Linq;
using DevExpress.Mvvm.DataAnnotations;
using DevExpress.Mvvm.Native;
using Telemart.Client.Core.Cloning;
using Telemart.Client.Helpers;
using Telemart.Client.Properties;
using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.Segment
{
    public class SegmentViewItem : TelemartEditorViewItemBase
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

        public bool Default
        {
            get { return GetProperty(() => Default); }
            set { SetProperty(() => Default, value); }
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

        public bool AutoShowcase
        {
            get { return GetProperty(() => AutoShowcase); }
            set { SetProperty(() => AutoShowcase, value); }
        }

        public ObservableCollection<SegmentCategoryFeatureViewItem> CategoryFeatures
        {
            get { return GetProperty(() => CategoryFeatures); }
            set { SetProperty(() => CategoryFeatures, value, () => RaisePropertyChanged(nameof(FeatureString))); }
        }

        public static void BuildMetadata(MetadataBuilder<SegmentViewItem> builder)
        {
            builder.Property(x => x.CategoryId).Required(() => Resources.RequiredErrorMessage);
            builder.Property(x => x.Name).Required(() => Resources.RequiredErrorMessage);
            builder.Property(x => x.EmployeeId).Required(() => Resources.RequiredErrorMessage);
        }

        public override object Clone()
        {
            SegmentViewItem item = ReflectionObjectCloner.Clone(this);

            item.CategoryFeatures = CategoryFeatures?.Select(x =>
            {
                object viewItem = x.Clone();

                return (SegmentCategoryFeatureViewItem)viewItem;
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