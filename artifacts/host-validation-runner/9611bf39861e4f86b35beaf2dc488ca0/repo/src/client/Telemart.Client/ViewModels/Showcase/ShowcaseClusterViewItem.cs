using System;
using System.Collections.ObjectModel;
using System.Linq;
using DevExpress.Mvvm.DataAnnotations;
using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.Showcase
{
    public class ShowcaseClusterViewItem : TelemartCloneableViewItemBase
    {
        public int Id
        {
            get { return GetProperty(() => Id); }
            set { SetProperty(() => Id, value); }
        }

        public int ClusterId
        {
            get { return GetProperty(() => ClusterId); }
            set { SetProperty(() => ClusterId, value); }
        }

        public int QuantityLocation
        {
            get { return GetProperty(() => QuantityLocation); }
            set { SetProperty(() => QuantityLocation, value, () => RaisePropertyChanged(nameof(QuantityClusterPlan))); }
        }

        public int QuantityClusterFact
        {
            get { return GetProperty(() => QuantityClusterFact); }
            set { SetProperty(() => QuantityClusterFact, value); }
        }

        public int CategoryId
        {
            get { return GetProperty(() => CategoryId); }
            set { SetProperty(() => CategoryId, value); }
        }

        public ObservableCollection<int> LocationIds
        {
            get { return GetProperty(() => LocationIds); }
            set { SetProperty(() => LocationIds, value); }
        }

        public DateTime ModifiedOn
        {
            get { return GetProperty(() => ModifiedOn); }
            set { SetProperty(() => ModifiedOn, value); }
        }

        public int ModifiedBy
        {
            get { return GetProperty(() => ModifiedBy); }
            set { SetProperty(() => ModifiedBy, value); }
        }

        public DateTime CreatedOn
        {
            get { return GetProperty(() => CreatedOn); }
            set { SetProperty(() => CreatedOn, value); }
        }

        public int CreatedBy
        {
            get { return GetProperty(() => CreatedBy); }
            set { SetProperty(() => CreatedBy, value); }
        }

        public int CountLocations => LocationIds?.Distinct().Count() ?? 0;

        public int QuantityClusterPlan => CountLocations * QuantityLocation;

        public static void BuildMetadata(MetadataBuilder<ShowcaseClusterViewItem> builder)
        {
            builder.Property(x => x.QuantityLocation)
                .MatchesRule(x => x >= 0 && x <= 1000, () => "Значение должно быть от 1 до 1000");
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Id, CategoryId, ClusterId, QuantityLocation);
        }

        public bool Equals(ShowcaseClusterViewItem other)
        {
            return Id == other?.Id && ClusterId == other.ClusterId && CategoryId == other.CategoryId && QuantityLocation == other.QuantityLocation;
        }

        public override bool Equals(object obj)
        {
            if (obj is null)
            {
                return false;
            }

            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            return obj is ShowcaseClusterViewItem item && Equals(item);
        }
    }
}