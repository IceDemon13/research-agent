using System;
using DevExpress.Mvvm;
using Telemart.Client.Dictionaries;

namespace Telemart.Client.ViewModels.Store.Purchase
{
    public abstract class SetSourceViewItemBase : BindableBase
    {
        public DateTime? DeliveryDateTime
        {
            get { return GetProperty(() => DeliveryDateTime); }
            set { SetProperty(() => DeliveryDateTime, value, RaisePropertiesChanged); }
        }

        public DateTime? OrderDeliveryDateTime
        {
            get { return GetProperty(() => OrderDeliveryDateTime); }
            set { SetProperty(() => OrderDeliveryDateTime, value, RaisePropertiesChanged); }
        }

        public OrderStatus OrderState
        {
            get { return GetProperty(() => OrderState); }
            set { SetProperty(() => OrderState, value, RaisePropertiesChanged); }
        }

        public PurchaseSourceSucceedInTimeState SucceedInTimeState => SucceedsInTime
            ? PurchaseSourceSucceedInTimeState.Ok
            : (OrderState == OrderStatus.Received
                ? PurchaseSourceSucceedInTimeState.Warning
                : PurchaseSourceSucceedInTimeState.Error);

        private bool SucceedsInTime => DeliveryDateTime == null || OrderDeliveryDateTime == null || DeliveryDateTime <= OrderDeliveryDateTime;

        private void RaisePropertiesChanged()
        {
            RaisePropertiesChanged(nameof(SucceedsInTime), nameof(SucceedInTimeState));
        }
    }
}
