using System;
using System.Collections.Generic;
using System.Linq;
using Telemart.Client.Core.Cloning;
using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.AdditionalServiceProduct
{
    public class AdditionalServiceProductViewItem : TelemartEditorViewItemBase
    {
        public int OrderId
        {
            get { return GetProperty(() => OrderId); }
            set { SetProperty(() => OrderId, value); }
        }

        public int StateId
        {
            get { return GetProperty(() => StateId); }
            set { SetProperty(() => StateId, value); }
        }

        public int OrderStateId
        {
            get { return GetProperty(() => OrderStateId); }
            set { SetProperty(() => OrderStateId, value); }
        }

        public int WarehouseId
        {
            get { return GetProperty(() => WarehouseId); }
            set { SetProperty(() => WarehouseId, value); }
        }

        public int OrderWarehouseId
        {
            get { return GetProperty(() => OrderWarehouseId); }
            set { SetProperty(() => OrderWarehouseId, value); }
        }

        public int EmployeeId
        {
            get { return GetProperty(() => EmployeeId); }
            set { SetProperty(() => EmployeeId, value); }
        }

        public DateTime? CompletedOn
        {
            get { return GetProperty(() => CompletedOn); }
            set { SetProperty(() => CompletedOn, value); }
        }

        public DateTime? Date
        {
            get { return GetProperty(() => Date); }
            set { SetProperty(() => Date, value); }
        }

        public int? CompletedBy
        {
            get { return GetProperty(() => CompletedBy); }
            set { SetProperty(() => CompletedBy, value); }
        }

        public DateTime? OrderDeliveryTimeTo
        {
            get { return GetProperty(() => OrderDeliveryTimeTo); }
            set { SetProperty(() => OrderDeliveryTimeTo, value); }
        }

        public string AdditionalServiceName
        {
            get { return GetProperty(() => AdditionalServiceName); }
            set { SetProperty(() => AdditionalServiceName, value); }
        }

        public bool ControlInMovements
        {
            get { return GetProperty(() => ControlInMovements); }
            set { SetProperty(() => ControlInMovements, value); }
        }

        public int? PrimaryAdditionalServiceProductId
        {
            get { return GetProperty(() => PrimaryAdditionalServiceProductId); }
            set { SetProperty(() => PrimaryAdditionalServiceProductId, value); }
        }

        public List<AdditionalServiceProductSnViewItem> ProductWithConsumables
        {
            get { return GetProperty(() => ProductWithConsumables); }
            set { SetProperty(() => ProductWithConsumables, value); }
        }

        public string GuestProduct
        {
            get { return GetProperty(() => GuestProduct); }
            set { SetProperty(() => GuestProduct, value); }
        }

        public string GuestProductSerialNumber
        {
            get { return GetProperty(() => GuestProductSerialNumber); }
            set { SetProperty(() => GuestProductSerialNumber, value); }
        }

        public string GuestProductDescription
        {
            get { return GetProperty(() => GuestProductDescription); }
            set { SetProperty(() => GuestProductDescription, value); }
        }

        public override object Clone()
        {
            AdditionalServiceProductViewItem item = ReflectionObjectCloner.Clone(this);

            item.ProductWithConsumables = ProductWithConsumables.Select(x =>
            {
                AdditionalServiceProductSnViewItem viewItem = ReflectionObjectCloner.Clone(x);
                viewItem.SerialNumber = x.SerialNumber;
                viewItem.Scanned = x.Scanned;
                return viewItem;
            }).ToList();

            return item;
        }
    }
}