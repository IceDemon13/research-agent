using System;
using DevExpress.Mvvm.DataAnnotations;
using Telemart.Client.Properties;
using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.Store.Order
{
    public class CourierDeliveryViewItem : TelemartViewItemBase
    {
        public CourierDeliveryViewItem(TimeSpan timeDeliveryFrom, TimeSpan timeDeliveryTo)
        {
            TimeDeliveryFrom = timeDeliveryFrom;
            TimeDeliveryTo = timeDeliveryTo;
            DisplayText = $"{timeDeliveryFrom:hh\\:mm} - {timeDeliveryTo:hh\\:mm}";
        }

        public string DisplayText
        {
            get { return GetProperty(() => DisplayText); }
            init { SetProperty(() => DisplayText, value); }
        }

        public TimeSpan TimeDeliveryFrom
        {
            get { return GetProperty(() => TimeDeliveryFrom); }
            init { SetProperty(() => TimeDeliveryFrom, value); }
        }

        public TimeSpan TimeDeliveryTo
        {
            get { return GetProperty(() => TimeDeliveryTo); }
            init { SetProperty(() => TimeDeliveryTo, value); }
        }
    }
}