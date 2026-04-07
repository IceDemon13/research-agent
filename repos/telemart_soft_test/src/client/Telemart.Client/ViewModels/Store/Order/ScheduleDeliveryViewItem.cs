using System;
using DevExpress.Mvvm.DataAnnotations;
using Telemart.Client.Properties;
using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.Store.Order
{
    public class ScheduleDeliveryViewItem : TelemartCloneableViewItemBase
    {
        public int OrderId
        {
            get { return GetProperty(() => OrderId); }
            init { SetProperty(() => OrderId, value); }
        }

        public string Address
        {
            get { return GetProperty(() => Address); }
            init { SetProperty(() => Address, value); }
        }

        public DateOnly DeliveryDateOld
        {
            get { return GetProperty(() => DeliveryDateOld); }
            init { SetProperty(() => DeliveryDateOld, value); }
        }

        public DateOnly DeliveryDateNew => DateX.HasValue ? DateOnly.FromDateTime(DateX!.Value) : DateOnly.FromDateTime(default);

        public TimeSpan DeliveryTime
        {
            get { return GetProperty(() => DeliveryTime); }
            set { SetProperty(() => DeliveryTime, value); }
        }

        public TimeSpan DeliveryTimeTo
        {
            get { return GetProperty(() => DeliveryTimeTo); }
            set { SetProperty(() => DeliveryTimeTo, value); }
        }

        public CourierDeliveryViewItem SelectedCourierDelivery
        {
            get { return GetProperty(() => SelectedCourierDelivery); }
            set { SetProperty(() => SelectedCourierDelivery, value, CourierDeliveryChanged); }
        }

        public int? SelectedCourierEmployeeId
        {
            get { return GetProperty(() => SelectedCourierEmployeeId); }
            set { SetProperty(() => SelectedCourierEmployeeId, value); }
        }

        public string CustomerPhone
        {
            get { return GetProperty(() => CustomerPhone); }
            set { SetProperty(() => CustomerPhone, value); }
        }

        public string Comment
        {
            get { return GetProperty(() => Comment); }
            set { SetProperty(() => Comment, value); }
        }

        public DateTime? DateX
        {
            get { return GetProperty(() => DateX); }
            set { SetProperty(() => DateX, value); }
        }

        public string CommentChangeDataX => DeliveryDateOld != DeliveryDateNew ? " Изменена дата Х" : string.Empty;

        public static void BuildMetadata(MetadataBuilder<ScheduleDeliveryViewItem> builder)
        {
            builder.Property(x => x.SelectedCourierDelivery).Required(() => Resources.RequiredErrorMessage);
            builder.Property(x => x.SelectedCourierEmployeeId).Required(() => Resources.RequiredErrorMessage);
            builder.Property(x => x.DateX).MatchesRule(x => x.HasValue, () => Resources.RequiredErrorMessage);
        }

        private void CourierDeliveryChanged()
        {
            if (SelectedCourierDelivery is null)
            {
                return;
            }

            DeliveryTime = SelectedCourierDelivery.TimeDeliveryFrom;
            DeliveryTimeTo = SelectedCourierDelivery.TimeDeliveryTo;
        }
    }
}