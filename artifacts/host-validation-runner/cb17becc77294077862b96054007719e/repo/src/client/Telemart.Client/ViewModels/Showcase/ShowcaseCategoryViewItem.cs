using System;
using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.Showcase
{
    public class ShowcaseCategoryViewItem : TelemartCloneableViewItemBase
    {
        public ShowcaseCategoryViewItem(
            int quantity,
            int categoryId,
            int warehouseId,
            string categoryName,
            string warehouseName,
            string placeName)
        {
            Quantity = quantity;
            PlaceName = placeName;
            WarehouseId = warehouseId;
            WarehouseName = warehouseName;
            CategoryId = categoryId;
            CategoryName = categoryName;
        }

        public ShowcaseCategoryViewItem()
        {
        }

        public int Id
        {
            get { return GetProperty(() => Id); }
            set { SetProperty(() => Id, value); }
        }

        public int Quantity
        {
            get { return GetProperty(() => Quantity); }
            set { SetProperty(() => Quantity, value, () => RaisePropertyChanged(nameof(IsChanged))); }
        }

        public int QuantityOld
        {
            get { return GetProperty(() => QuantityOld); }
            set { SetProperty(() => QuantityOld, value); }
        }

        public int WarehouseId
        {
            get { return GetProperty(() => WarehouseId); }
            set { SetProperty(() => WarehouseId, value, () => RaisePropertyChanged(nameof(IsChanged))); }
        }

        public int WarehouseOldId
        {
            get { return GetProperty(() => WarehouseOldId); }
            set { SetProperty(() => WarehouseOldId, value); }
        }

        public string WarehouseName
        {
            get { return GetProperty(() => WarehouseName); }
            set { SetProperty(() => WarehouseName, value); }
        }

        public int CategoryId
        {
            get { return GetProperty(() => CategoryId); }
            set { SetProperty(() => CategoryId, value, () => RaisePropertyChanged(nameof(IsChanged))); }
        }

        public int CategoryOldId
        {
            get { return GetProperty(() => CategoryOldId); }
            set { SetProperty(() => CategoryOldId, value); }
        }

        public string CategoryName
        {
            get { return GetProperty(() => CategoryName); }
            set { SetProperty(() => CategoryName, value); }
        }

        public string PlaceName
        {
            get { return GetProperty(() => PlaceName); }
            set { SetProperty(() => PlaceName, value); }
        }

        public int? ClusterId
        {
            get { return GetProperty(() => ClusterId); }
            set { SetProperty(() => ClusterId, value); }
        }

        public int? LocationId
        {
            get { return GetProperty(() => LocationId); }
            set { SetProperty(() => LocationId, value); }
        }

        public int QuantityLocation
        {
            get { return GetProperty(() => QuantityLocation); }
            set { SetProperty(() => QuantityLocation, value, () => RaisePropertiesChanged(nameof(QuantityClusterPlan), nameof(DifferenceLocation))); }
        }

        public int CountLocations
        {
            get { return GetProperty(() => CountLocations); }
            set { SetProperty(() => CountLocations, value, () => RaisePropertyChanged(nameof(QuantityClusterPlan))); }
        }

        public int QuantityClusterFact
        {
            get { return GetProperty(() => QuantityClusterFact); }
            set { SetProperty(() => QuantityClusterFact, value, () => RaisePropertyChanged(nameof(DifferenceCluster))); }
        }

        public int QuantityLocationFact
        {
            get { return GetProperty(() => QuantityLocationFact); }
            set { SetProperty(() => QuantityLocationFact, value, () => RaisePropertyChanged(nameof(DifferenceLocation))); }
        }

        public double AverageQuantityLocation
        {
            get { return GetProperty(() => AverageQuantityLocation); }
            set { SetProperty(() => AverageQuantityLocation, value); }
        }

        public int QuantityClusterPlan => ClusterId.HasValue ? CountLocations * QuantityLocation : 0;

        public int DifferenceCluster => ClusterId.HasValue ? QuantityClusterPlan - QuantityClusterFact : 0;

        public int DifferenceLocation => QuantityLocation - QuantityLocationFact;

        public bool IsChanged => Quantity != QuantityOld || CategoryId != CategoryOldId || WarehouseId != WarehouseOldId;

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

            return obj is ShowcaseCategoryViewItem item && Equals(item);
        }

        public bool Equals(ShowcaseCategoryViewItem other)
        {
            return Id == other.Id
                   && CategoryOldId == other.CategoryOldId
                   && CategoryId == other.CategoryId
                   && WarehouseOldId == other.WarehouseOldId
                   && WarehouseId == other.WarehouseId
                   && QuantityOld == other.QuantityOld
                   && Quantity == other.Quantity
                   && PlaceName == other.PlaceName;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Id, CategoryId, WarehouseId, Quantity, PlaceName);
        }
    }
}