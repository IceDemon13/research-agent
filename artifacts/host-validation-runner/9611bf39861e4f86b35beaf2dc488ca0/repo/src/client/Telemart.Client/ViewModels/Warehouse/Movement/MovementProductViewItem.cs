using System;
using System.Collections.Generic;
using System.Linq;
using DevExpress.Mvvm;
using Telemart.Client.Core.Cloning;
using Telemart.Client.Core.Extensions;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Common.Localization;

namespace Telemart.Client.ViewModels.Warehouse.Movement
{
    public sealed class MovementProductViewItem : BindableBase, IEquatable<MovementProductViewItem>, ILocalіzableEntity
    {
        public MovementProductViewItem()
        {
            RowRef = Guid.NewGuid();
        }

        public Guid RowRef
        {
            get { return GetProperty(() => RowRef); }
            set { SetProperty(() => RowRef, value); }
        }

        public Guid? ParentRowRef
        {
            get { return GetProperty(() => ParentRowRef); }
            set { SetProperty(() => ParentRowRef, value); }
        }

        public int ProductId
        {
            get { return GetProperty(() => ProductId); }
            set { SetProperty(() => ProductId, value); }
        }

        public string ProductName
        {
            get { return GetProperty(() => ProductName); }
            set { SetProperty(() => ProductName, value, () => { RaisePropertyChanged(nameof(ProductFullName)); }); }
        }

        public string ProductNameUa
        {
            get { return GetProperty(() => ProductNameUa); }
            set { SetProperty(() => ProductNameUa, value, () => { RaisePropertyChanged(nameof(ProductFullName)); }); }
        }

        public string ProductNameEn
        {
            get { return GetProperty(() => ProductNameEn); }
            set { SetProperty(() => ProductNameEn, value, () => { RaisePropertyChanged(nameof(ProductFullName)); }); }
        }

        public string ProductNameBase
        {
            get { return GetProperty(() => ProductNameBase); }
            set { SetProperty(() => ProductNameBase, value); }
        }

        public string ProductPrefixRus
        {
            get { return GetProperty(() => ProductPrefixRus); }
            set { SetProperty(() => ProductPrefixRus, value, () => { RaisePropertyChanged(nameof(ProductFullName)); }); }
        }

        public string ProductPrefixUa
        {
            get { return GetProperty(() => ProductPrefixUa); }
            set { SetProperty(() => ProductPrefixUa, value, () => { RaisePropertyChanged(nameof(ProductFullName)); }); }
        }

        public string ProductPrefixEn
        {
            get { return GetProperty(() => ProductPrefixEn); }
            set { SetProperty(() => ProductPrefixEn, value, () => { RaisePropertyChanged(nameof(ProductFullName)); }); }
        }

        public int Quantity
        {
            get { return GetProperty(() => Quantity); }
            set { SetProperty(() => Quantity, value, () => RaisePropertyChanged(nameof(IsQuantityOutDiffersFromQuantity))); }
        }

        public int QuantityIn
        {
            get { return GetProperty(() => QuantityIn); }
            set { SetProperty(() => QuantityIn, value, () => RaisePropertyChanged(nameof(IsQuantityInDiffersFromQuantityOut))); }
        }

        public int QuantityOut
        {
            get { return GetProperty(() => QuantityOut); }
            set { SetProperty(() => QuantityOut, value, () => RaisePropertiesChanged(nameof(IsQuantityOutDiffersFromQuantity), nameof(IsQuantityInDiffersFromQuantityOut))); }
        }

        public decimal? Price
        {
            get { return GetProperty(() => Price); }
            set { SetProperty(() => Price, value); }
        }

        public int UsdCurrency
        {
            get { return GetProperty(() => UsdCurrency); }
            set { SetProperty(() => UsdCurrency, value); }
        }

        public int ProductParentCategoryId
        {
            get { return GetProperty(() => ProductParentCategoryId); }
            set { SetProperty(() => ProductParentCategoryId, value); }
        }

        public string ProductParentCategoryName
        {
            get { return GetProperty(() => ProductParentCategoryName); }
            set { SetProperty(() => ProductParentCategoryName, value); }
        }

        public int ProductParentCategoryLeft
        {
            get { return GetProperty(() => ProductParentCategoryLeft); }
            set { SetProperty(() => ProductParentCategoryLeft, value); }
        }

        public int? CreatedBy
        {
            get { return GetProperty(() => CreatedBy); }
            set { SetProperty(() => CreatedBy, value); }
        }

        public double Weight
        {
            get { return GetProperty(() => Weight); }
            set { SetProperty(() => Weight, value); }
        }

        public int TypeId
        {
            get { return GetProperty(() => TypeId); }
            set { SetProperty(() => TypeId, value); }
        }

        public int AdditionalServicesQuantity
        {
            get { return GetProperty(() => AdditionalServicesQuantity); }
            set { SetProperty(() => AdditionalServicesQuantity, value); }
        }

        public string GroupString
        {
            get { return GetProperty(() => GroupString); }
            set { SetProperty(() => GroupString, value); }
        }

        public int? AssemblyServiceId
        {
            get { return GetProperty(() => AssemblyServiceId); }
            set { SetProperty(() => AssemblyServiceId, value); }
        }

        public int? AssemblyServiceProductId
        {
            get { return GetProperty(() => AssemblyServiceProductId); }
            set { SetProperty(() => AssemblyServiceProductId, value); }
        }

        public int? AdditionalServiceProductId
        {
            get { return GetProperty(() => AdditionalServiceProductId); }
            set { SetProperty(() => AdditionalServiceProductId, value, () => RaisePropertyChanged(nameof(IsAdditionalServiceProduct))); }
        }

        public string AdditionalServiceProductToolTip
        {
            get { return GetProperty(() => AdditionalServiceProductToolTip); }
            set { SetProperty(() => AdditionalServiceProductToolTip, value); }
        }

        public bool ManualAdded
        {
            get { return GetProperty(() => ManualAdded); }
            set { SetProperty(() => ManualAdded, value); }
        }

        public bool AssembledComputerForOrder
        {
            get { return GetProperty(() => AssembledComputerForOrder); }
            set { SetProperty(() => AssembledComputerForOrder, value); }
        }

        public bool IsConsumableAdditionalServiceProduct
        {
            get { return GetProperty(() => IsConsumableAdditionalServiceProduct); }
            set { SetProperty(() => IsConsumableAdditionalServiceProduct, value); }
        }

        public string AssembledComputerForOrderToolTip
        {
            get { return GetProperty(() => AssembledComputerForOrderToolTip); }
            set { SetProperty(() => AssembledComputerForOrderToolTip, value); }
        }

        public List<MovementProductSnViewItem> SerialNumbers
        {
            get { return GetProperty(() => SerialNumbers); }
            set { SetProperty(() => SerialNumbers, value, RaiseProperties); }
        }

        public IReadOnlyCollection<int> OrderIds
        {
            get { return GetProperty(() => OrderIds); }
            set { SetProperty(() => OrderIds, value); }
        }

        public string AdditionalServiceProductSerialNumber
        {
            get { return GetProperty(() => AdditionalServiceProductSerialNumber); }
            set { SetProperty(() => AdditionalServiceProductSerialNumber, value); }
        }

        public string PrimaryAdditionalServiceProductSerialNumber
        {
            get { return GetProperty(() => PrimaryAdditionalServiceProductSerialNumber); }
            set { SetProperty(() => PrimaryAdditionalServiceProductSerialNumber, value); }
        }

        public bool IsAdditionalServiceProduct => AdditionalServiceProductId.HasValue;

        public bool IsQuantityOutDiffersFromQuantity => QuantityOut != Quantity;

        public bool IsQuantityInDiffersFromQuantityOut => QuantityIn != QuantityOut;

        public string ProductFullName => this.GetLocalName(LocalizableNameType.Ukr);

        public bool AnyQuantityInSerials => SerialNumbers?.Any(x => x.ScannedIn) == true;

        public bool AnyQuantityOutSerials => SerialNumbers?.Any(x => x.ScannedOut) == true;

        public string Name => ProductName.GetStringWithPrefix(ProductPrefixRus);

        public string NameUkr => ProductNameUa.GetStringWithPrefix(ProductPrefixUa);

        public string NameEn => ProductNameEn.GetStringWithPrefix(ProductPrefixEn);

        public static bool operator !=(MovementProductViewItem left, MovementProductViewItem right)
        {
            return !Equals(left, right);
        }

        public static bool operator ==(MovementProductViewItem left, MovementProductViewItem right)
        {
            return Equals(left, right);
        }

        public override bool Equals(object obj)
        {
            if (obj is null)
            {
                return false;
            }

            return ReferenceEquals(this, obj) || Equals(obj as MovementProductViewItem);
        }

        public bool Equals(MovementProductViewItem other)
        {
            return other != null
                   && GroupString == other.GroupString
                   && AssemblyServiceId == other.AssemblyServiceId
                   && ProductId.Equals(other.ProductId)
                   && ProductName.Equals(other.ProductName)
                   && Quantity.Equals(other.Quantity)
                   && QuantityIn.Equals(other.QuantityIn)
                   && QuantityOut.Equals(other.QuantityOut)
                   && AdditionalServicesQuantity == other.AdditionalServicesQuantity
                   && IsConsumableAdditionalServiceProduct == other.IsConsumableAdditionalServiceProduct
                   && RowRef == other.RowRef;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(base.GetHashCode(), ProductId, Quantity, QuantityIn, QuantityOut, RowRef);
        }

        public MovementProductViewItem SplitAssemblyService(int splittedQuantity, bool scannedIn, bool scannedOut)
        {
            if (Quantity <= splittedQuantity)
            {
                return this;
            }

            MovementProductViewItem splittedMovementProduct = ReflectionObjectCloner.Clone(this);

            splittedMovementProduct.RowRef = Guid.NewGuid();
            Quantity -= splittedQuantity;
            splittedMovementProduct.Quantity = splittedQuantity;

            if (scannedIn)
            {
                splittedMovementProduct.QuantityIn = splittedQuantity;
                QuantityIn -= splittedQuantity;
            }

            if (scannedOut)
            {
                splittedMovementProduct.QuantityOut = splittedQuantity;
                QuantityOut -= splittedQuantity;
            }

            return splittedMovementProduct;
        }

        public MovementProductViewItem SplitConsumableAdditionalServiceProduct(
            bool additionalServiceScannedIn,
            bool additionalServiceScannedOut,
            string groupString,
            Guid parentRowRef)
        {
            const int consumableProductQuantity = 1;
            bool lastItem = Quantity == consumableProductQuantity;

            MovementProductViewItem splittedMovementProduct;

            if (lastItem)
            {
                splittedMovementProduct = this;
            }
            else
            {
                splittedMovementProduct = ReflectionObjectCloner.Clone(this);
                splittedMovementProduct.RowRef = Guid.NewGuid();
            }

            splittedMovementProduct.ParentRowRef = parentRowRef;
            splittedMovementProduct.GroupString = groupString;

            if (lastItem)
            {
                splittedMovementProduct.QuantityIn = additionalServiceScannedIn ? consumableProductQuantity : 0;
                splittedMovementProduct.QuantityOut = additionalServiceScannedOut ? consumableProductQuantity : 0;
                splittedMovementProduct.IsConsumableAdditionalServiceProduct = true;
            }
            else
            {
                splittedMovementProduct.Quantity = consumableProductQuantity;
                splittedMovementProduct.QuantityIn = additionalServiceScannedIn ? consumableProductQuantity : 0;
                splittedMovementProduct.QuantityOut = additionalServiceScannedOut ? consumableProductQuantity : 0;
                splittedMovementProduct.IsConsumableAdditionalServiceProduct = true;

                Quantity -= splittedMovementProduct.Quantity;
                QuantityIn -= splittedMovementProduct.QuantityIn;
                QuantityOut -= splittedMovementProduct.QuantityOut;
            }

            return splittedMovementProduct;
        }

        public MovementProductViewItem SplitAdditionalService(bool scannedIn, bool scannedOut)
        {
            MovementProductViewItem splittedMovementProduct = ReflectionObjectCloner.Clone(this);

            Quantity--;

            splittedMovementProduct.RowRef = Guid.NewGuid();
            splittedMovementProduct.Quantity = 1;
            splittedMovementProduct.QuantityIn = 0;
            splittedMovementProduct.QuantityOut = 0;
            splittedMovementProduct.SerialNumbers = new List<MovementProductSnViewItem>(SerialNumbers);

            if (scannedIn)
            {
                splittedMovementProduct.QuantityIn++;
                QuantityIn--;
            }

            if (scannedOut)
            {
                splittedMovementProduct.QuantityOut++;
                QuantityOut--;
            }

            return splittedMovementProduct;
        }

        public MovementProductViewItem SplitAssembledComputer(IReadOnlyCollection<string> nomenclatureSeriesFromOrders, int movementStateId)
        {
            int quantityForOrders = nomenclatureSeriesFromOrders.Count;

            if (Quantity == quantityForOrders && (SerialNumbers is null || SerialNumbers.All(x => nomenclatureSeriesFromOrders.Contains(x.NomenclatureSeries))))
            {
                return this;
            }

            MovementProductViewItem assembledComputerForOrder = ReflectionObjectCloner.Clone(this);

            assembledComputerForOrder.SerialNumbers = new List<MovementProductSnViewItem>();

            assembledComputerForOrder.Quantity = quantityForOrders;
            assembledComputerForOrder.QuantityIn = 0;
            assembledComputerForOrder.QuantityOut = 0;
            Quantity -= quantityForOrders;

            if (SerialNumbers?.Any() == true)
            {
                List<MovementProductSnViewItem> nomenclaturesSeriesToRemoveFromCurrentAndAddToSplittedProduct =
                    new List<MovementProductSnViewItem>();

                foreach (MovementProductSnViewItem scannedNomenclaturesSeries in SerialNumbers)
                {
                    if (nomenclatureSeriesFromOrders.Contains(scannedNomenclaturesSeries.NomenclatureSeries))
                    {
                        if (scannedNomenclaturesSeries.ScannedIn)
                        {
                            QuantityIn--;
                            assembledComputerForOrder.QuantityIn++;
                        }

                        if (scannedNomenclaturesSeries.ScannedOut)
                        {
                            QuantityOut--;
                            assembledComputerForOrder.QuantityOut++;
                        }

                        nomenclaturesSeriesToRemoveFromCurrentAndAddToSplittedProduct.Add(scannedNomenclaturesSeries);
                    }
                }

                foreach (MovementProductSnViewItem nomenclatureSeries in
                         nomenclaturesSeriesToRemoveFromCurrentAndAddToSplittedProduct)
                {
                    SerialNumbers.Remove(nomenclatureSeries);
                    assembledComputerForOrder.SerialNumbers.Add(nomenclatureSeries);
                }
            }
            else
            {
                int movedQuantityIn = Math.Min(QuantityIn, quantityForOrders);
                int movedQuantityOut = Math.Min(QuantityOut, quantityForOrders);
                QuantityIn -= movedQuantityIn;
                QuantityOut -= movedQuantityOut;
                assembledComputerForOrder.QuantityIn = movedQuantityIn;
                assembledComputerForOrder.QuantityOut = movedQuantityOut;
            }

            RaiseProperties();

            return assembledComputerForOrder;
        }

        public void DecrementProductForAdditionalServices(bool allAdditionalServicesScannedIn, bool allAdditionalServicesScannedOut)
        {
            Quantity--;

            if (allAdditionalServicesScannedIn)
            {
                QuantityIn--;
            }

            if (allAdditionalServicesScannedOut)
            {
                QuantityOut--;
            }
        }

        public void Scan(int movementStateId, int quantity)
        {
            if (movementStateId == MovementState.New.Id)
            {
                QuantityOut += quantity;
            }
            else if (movementStateId == MovementState.Arrived.Id)
            {
                QuantityIn += quantity;
            }
        }

        public void ScanAssemblyProduct(int movementStateId)
        {
            if (movementStateId == MovementState.New.Id)
            {
                QuantityOut = Quantity;
            }
            else if (movementStateId == MovementState.Arrived.Id)
            {
                QuantityIn = Quantity;
            }
        }

        public bool IsScanned(int movementStateId)
        {
            if (movementStateId == MovementState.New.Id)
            {
                return QuantityOut >= Quantity;
            }

            if (movementStateId == MovementState.Arrived.Id)
            {
                return QuantityIn >= Quantity;
            }

            return false;
        }

        public void RefreshSerialNumbersAfterEditing(IReadOnlyCollection<string> serialNumbersFromEditForm, int movementStateId)
        {
            List<MovementProductSnViewItem> serialNumbersToDelete = new List<MovementProductSnViewItem>();

            int decrementQuantity = 0;

            foreach (MovementProductSnViewItem currentSerialNumber in SerialNumbers)
            {
                string serialNumberFromEditForm = serialNumbersFromEditForm.FirstOrDefault(x => x == currentSerialNumber.Sn);

                if (serialNumberFromEditForm is not null)
                {
                    continue;
                }

                serialNumbersToDelete.Add(currentSerialNumber);

                decrementQuantity++;
            }

            serialNumbersToDelete.ForEach(x => SerialNumbers.Remove(x));

            if (movementStateId == MovementState.New.Id)
            {
                QuantityOut -= decrementQuantity;
                QuantityOut = Math.Max(QuantityOut, 0);
            }
            else if (movementStateId == MovementState.Arrived.Id)
            {
                QuantityIn -= decrementQuantity;
                QuantityIn = Math.Max(QuantityIn, 0);
            }

            RaiseProperties();
        }

        public void RaiseProperties()
        {
            RaisePropertiesChanged(nameof(AnyQuantityInSerials), nameof(AnyQuantityOutSerials));
        }
    }
}