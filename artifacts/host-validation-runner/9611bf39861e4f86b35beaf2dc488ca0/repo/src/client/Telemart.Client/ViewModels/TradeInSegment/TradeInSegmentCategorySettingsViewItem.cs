using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using DevExpress.Mvvm.Native;
using Telemart.Client.Core.Cloning;
using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.TradeInSegment
{
    public class TradeInSegmentCategorySettingsViewItem : TelemartCloneableViewItemBase
    {
        public TradeInSegmentCategorySettingsViewItem(
            int categoryId,
            int parentCategoryId,
            string categoryName,
            int categoryEmployeeId,
            bool allowEdit,
            bool filledInSegments,
            ObservableCollection<TradeInSegmentCategorySettingsFeatureViewItem> features)
        {
            CategoryId = categoryId;
            CategoryName = categoryName;
            Features = features;
            DisplayFeatures = features.Cast<object>().ToList();
            AllowEdit = allowEdit;
            FilledInSegments = filledInSegments;
            CategoryEmployeeId = categoryEmployeeId;
            ParentCategoryId = parentCategoryId;
        }

        public TradeInSegmentCategorySettingsViewItem()
        {
        }

        public int CategoryId
        {
            get { return GetProperty(() => CategoryId); }
            set { SetProperty(() => CategoryId, value); }
        }

        public int ParentCategoryId
        {
            get { return GetProperty(() => ParentCategoryId); }
            set { SetProperty(() => ParentCategoryId, value); }
        }

        public bool AllowEdit
        {
            get { return GetProperty(() => AllowEdit); }
            set { SetProperty(() => AllowEdit, value); }
        }

        public bool FilledInSegments
        {
            get { return GetProperty(() => FilledInSegments); }
            set { SetProperty(() => FilledInSegments, value); }
        }

        public string CategoryName
        {
            get { return GetProperty(() => CategoryName); }
            set { SetProperty(() => CategoryName, value); }
        }

        public int CategoryEmployeeId
        {
            get { return GetProperty(() => CategoryEmployeeId); }
            set { SetProperty(() => CategoryEmployeeId, value); }
        }

        public ObservableCollection<TradeInSegmentCategorySettingsFeatureViewItem> Features
        {
            get { return GetProperty(() => Features); }
            set { SetProperty(() => Features, value); }
        }

        public List<object> DisplayFeatures
        {
            get { return GetProperty(() => DisplayFeatures); }
            set { SetProperty(() => DisplayFeatures, value); }
        }

        public override object Clone()
        {
            TradeInSegmentCategorySettingsViewItem item = ReflectionObjectCloner.Clone(this);

            item.Features = Features.Select(x =>
            {
                TradeInSegmentCategorySettingsFeatureViewItem viewItem = ReflectionObjectCloner.Clone(x);
                return viewItem;
            }).ToObservableCollection();

            item.DisplayFeatures = new List<object>(DisplayFeatures);

            return item;
        }
    }
}