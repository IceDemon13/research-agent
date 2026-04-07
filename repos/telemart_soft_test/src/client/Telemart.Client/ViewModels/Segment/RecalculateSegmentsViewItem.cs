using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.Segment
{
    public class RecalculateSegmentsViewItem : TelemartViewItemBase
    {
        public RecalculateSegmentsViewItem(
            int parentCategoryId,
            string parentCategoryName,
            int segmentId,
            string segmentName,
            int calculatedProductsQuantity,
            bool noSegment = false,
            bool noSegmentNotFilledFeatures = false)
        {
            ParentCategoryId = parentCategoryId;
            ParentCategoryName = parentCategoryName;
            SegmentId = segmentId;
            SegmentName = segmentName;
            CalculatedProductsQuantity = calculatedProductsQuantity;
            NoSegment = noSegment;
            NoSegmentNotFilledFeatures = noSegmentNotFilledFeatures;
        }

        public int ParentCategoryId
        {
            get { return GetProperty(() => ParentCategoryId); }
            set { SetProperty(() => ParentCategoryId, value); }
        }

        public string ParentCategoryName
        {
            get { return GetProperty(() => ParentCategoryName); }
            set { SetProperty(() => ParentCategoryName, value); }
        }

        public int SegmentId
        {
            get { return GetProperty(() => SegmentId); }
            set { SetProperty(() => SegmentId, value); }
        }

        public string SegmentName
        {
            get { return GetProperty(() => SegmentName); }
            set { SetProperty(() => SegmentName, value); }
        }

        public int CalculatedProductsQuantity
        {
            get { return GetProperty(() => CalculatedProductsQuantity); }
            set { SetProperty(() => CalculatedProductsQuantity, value); }
        }

        public bool NoSegment
        {
            get { return GetProperty(() => NoSegment); }
            set { SetProperty(() => NoSegment, value); }
        }

        public bool NoSegmentNotFilledFeatures
        {
            get { return GetProperty(() => NoSegmentNotFilledFeatures); }
            set { SetProperty(() => NoSegmentNotFilledFeatures, value); }
        }
    }
}