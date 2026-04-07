using System;
using DevExpress.Mvvm;
using Telemart.Client.Business;
using Telemart.Client.Dictionaries;

namespace Telemart.Client.ViewModels.Store.CreateScanSheet
{
    public class ScanSheetOrderViewItem : BindableBase
    {
        public ScanSheetOrderViewItem() { }

        public DateTime? DeliveryTime
        {
            get { return GetProperty(() => DeliveryTime); }
            set { SetProperty(() => DeliveryTime, value); }
        }

        public int Id
        {
            get { return GetProperty(() => Id); }
            set { SetProperty(() => Id, value); }
        }

        public int? CourierEmployeeId
        {
            get { return GetProperty(() => CourierEmployeeId); }
            set { SetProperty(() => CourierEmployeeId, value); }
        }

        public Subdivision Subdivision
        {
            get { return GetProperty(() => Subdivision); }
            set { SetProperty(() => Subdivision, value); }
        }

        public CarryType Carry
        {
            get { return GetProperty(() => Carry); }
            set { SetProperty(() => Carry, value); }
        }

        public int PackagePlaces
        {
            get { return GetProperty(() => PackagePlaces); }
            set { SetProperty(() => PackagePlaces, value); }
        }

        public string Fio
        {
            get { return GetProperty(() => Fio); }
            set { SetProperty(() => Fio, value); }
        }

        public string Phone
        {
            get { return GetProperty(() => Phone); }
            set { SetProperty(() => Phone, value); }
        }

        public string Phone2
        {
            get { return GetProperty(() => Phone2); }
            set { SetProperty(() => Phone2, value); }
        }

        public string Address
        {
            get { return GetProperty(() => Address); }
            set { SetProperty(() => Address, value); }
        }

        public string Comment
        {
            get { return GetProperty(() => Comment); }
            set { SetProperty(() => Comment, value); }
        }

        public Prices PriceToCost
        {
            get { return GetProperty(() => PriceToCost); }
            set { SetProperty(() => PriceToCost, value); }
        }
    }
}
