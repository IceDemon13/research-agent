using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using DevExpress.Mvvm;
using DevExpress.Mvvm.DataAnnotations;
using DevExpress.Mvvm.Native;
using Telemart.Client.Core.Cloning;
using Telemart.Client.Dictionaries;
using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.Service.ServiceMovements
{
    public class ServiceMovementViewItem : BindableBase, ILockableEntity, IDataErrorInfo, ICloneable
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

        public DateTime DateReceive
        {
            get { return GetProperty(() => DateReceive); }
            set { SetProperty(() => DateReceive, value); }
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

        public MovementState State
        {
            get { return GetProperty(() => State); }
            set { SetProperty(() => State, value); }
        }

        public ObservableCollection<ServiceMovementProductViewItem> Products
        {
            get { return GetProperty(() => Products); }
            set { SetProperty(() => Products, value); }
        }

        public int? EmployeeLockId
        {
            get { return GetProperty(() => EmployeeLockId); }
            set { SetProperty(() => EmployeeLockId, value); }
        }

        public string EmployeeLockName
        {
            get { return GetProperty(() => EmployeeLockName); }
            set { SetProperty(() => EmployeeLockName, value); }
        }

        public int? DeliveryTypeId
        {
            get { return GetProperty(() => DeliveryTypeId); }
            set { SetProperty(() => DeliveryTypeId, value); }
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

        public short? Places
        {
            get { return GetProperty(() => Places); }
            set { SetProperty(() => Places, value); }
        }

        public string TrackNumber
        {
            get { return GetProperty(() => TrackNumber); }
            set { SetProperty(() => TrackNumber, value); }
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

        string IDataErrorInfo.Error => string.Empty;

        string IDataErrorInfo.this[string columnName] => IDataErrorInfoHelper.GetErrorText(this, columnName);

        public static void BuildMetadata(MetadataBuilder<ServiceMovementViewItem> builder)
        {
        }

        object ICloneable.Clone()
        {
            return Clone();
        }

        public ServiceMovementViewItem Clone()
        {
            ServiceMovementViewItem item = ReflectionObjectCloner.Clone(this);

            item.Products = Products.Select(x => ReflectionObjectCloner.Clone(x)).ToObservableCollection();

            return item;
        }
    }
}