using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using Telemart.Client.Core.Cloning;
using Telemart.Client.Dictionaries;
using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.Warehouse.Movement
{
    public sealed class MovementViewItem : BindableBase, IDataErrorInfo, ICloneable, ILockableEntity
    {
        public int Id
        {
            get { return GetProperty(() => Id); }
            set { SetProperty(() => Id, value); }
        }

        public int WarehouseFromId
        {
            get { return GetProperty(() => WarehouseFromId); }
            set { SetProperty(() => WarehouseFromId, value); }
        }

        public int WarehouseToId
        {
            get { return GetProperty(() => WarehouseToId); }
            set { SetProperty(() => WarehouseToId, value); }
        }

        public DateTime DateOut
        {
            get { return GetProperty(() => DateOut); }
            set { SetProperty(() => DateOut, value); }
        }

        public DateTime DateDeparture
        {
            get { return GetProperty(() => DateDeparture); }
            set { SetProperty(() => DateDeparture, value); }
        }

        public DateTime DateArrive
        {
            get { return GetProperty(() => DateArrive); }
            set { SetProperty(() => DateArrive, value); }
        }

        public DateTime DateIn
        {
            get { return GetProperty(() => DateIn); }
            set { SetProperty(() => DateIn, value); }
        }

        public MovementState State
        {
            get { return GetProperty(() => State); }
            set { SetProperty(() => State, value); }
        }

        public int? CarryId
        {
            get { return GetProperty(() => CarryId); }
            set { SetProperty(() => CarryId, value); }
        }

        public CarryType Carry
        {
            get { return GetProperty(() => Carry); }
            set { SetProperty(() => Carry, value, () => CarryId = Carry?.Id); }
        }

        public int? Places
        {
            get { return GetProperty(() => Places); }
            set { SetProperty(() => Places, value); }
        }

        public string TrackNumber
        {
            get { return GetProperty(() => TrackNumber); }
            set { SetProperty(() => TrackNumber, value); }
        }

        public int[] OrderIds
        {
            get { return GetProperty(() => OrderIds); }
            set { SetProperty(() => OrderIds, value); }
        }

        public int? EmployeeLockId
        {
            get { return GetProperty(() => EmployeeLockId); }
            set { SetProperty(() => EmployeeLockId, value); }
        }

        public DateTime CreatedOn
        {
            get { return GetProperty(() => CreatedOn); }
            set { SetProperty(() => CreatedOn, value); }
        }

        public DateTime? ArrivedOn
        {
            get { return GetProperty(() => ArrivedOn); }
            set { SetProperty(() => ArrivedOn, value); }
        }

        public int CreatedBy
        {
            get { return GetProperty(() => CreatedBy); }
            set { SetProperty(() => CreatedBy, value); }
        }

        public DateTime? SentOn
        {
            get { return GetProperty(() => SentOn); }
            set { SetProperty(() => SentOn, value); }
        }

        public int? SentBy
        {
            get { return GetProperty(() => SentBy); }
            set { SetProperty(() => SentBy, value); }
        }

        public DateTime? ReceivedOn
        {
            get { return GetProperty(() => ReceivedOn); }
            set { SetProperty(() => ReceivedOn, value); }
        }

        public int? ReceivedBy
        {
            get { return GetProperty(() => ReceivedBy); }
            set { SetProperty(() => ReceivedBy, value); }
        }

        public string EmployeeLockName
        {
            get { return GetProperty(() => EmployeeLockName); }
            set { SetProperty(() => EmployeeLockName, value); }
        }

        public bool? SourceCurrentDateX
        {
            get { return GetProperty(() => SourceCurrentDateX); }
            set { SetProperty(() => SourceCurrentDateX, value); }
        }

        public string NpCourierCallBarcode
        {
            get { return GetProperty(() => NpCourierCallBarcode); }
            set { SetProperty(() => NpCourierCallBarcode, value); }
        }

        public string NpCourierCallInterval
        {
            get { return GetProperty(() => NpCourierCallInterval); }
            set { SetProperty(() => NpCourierCallInterval, value); }
        }

        public double? ProductsWeightFact => MovementProducts?.Sum(x => x.Weight * x.QuantityOut);

        public double? ProductsWeightPlan => MovementProducts?.Sum(x => x.Weight * x.Quantity);


        public int StateId => State.Id;

        public ObservableCollection<MovementProductViewItem> MovementProducts
        {
            get { return GetProperty(() => MovementProducts); }
            set { SetProperty(() => MovementProducts, value, () => RaisePropertiesChanged(nameof(ProductsWeightPlan), nameof(ProductsWeightFact))); }
        }

        public ObservableCollection<WarehouseRouteTimePurpose> Purposes
        {
            get { return GetProperty(() => Purposes); }
            set { SetProperty(() => Purposes, value); }
        }

        public string PurposesText => Purposes is null ? null : string.Join(", ", Purposes.Select(x => x.Name));

        #region IDataErrorInfo

        string IDataErrorInfo.Error => string.Empty;

        string IDataErrorInfo.this[string columnName] => IDataErrorInfoHelper.GetErrorText(this, columnName);

        #endregion

        public MovementViewItem Clone()
        {
            MovementViewItem clonedItem = ReflectionObjectCloner.Clone(this);

            clonedItem.MovementProducts = MovementProducts.Select(x => ReflectionObjectCloner.Clone(x)).ToObservableCollection();

            return clonedItem;
        }

        object ICloneable.Clone()
        {
            return Clone();
        }
    }
}