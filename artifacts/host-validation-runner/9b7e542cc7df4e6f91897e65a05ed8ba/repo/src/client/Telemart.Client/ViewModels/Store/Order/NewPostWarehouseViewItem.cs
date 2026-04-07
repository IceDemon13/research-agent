using System;
using DevExpress.Mvvm;

namespace Telemart.Client.ViewModels.Store.Order
{
    public sealed class NewPostWarehouseViewItem : BindableBase
    {
        public Guid Ref
        {
            get { return GetProperty(() => Ref); }
            set { SetProperty(() => Ref, value); }
        }

        public int Number
        {
            get { return GetProperty(() => Number); }
            set { SetProperty(() => Number, value); }
        }

        public string Description
        {
            get { return GetProperty(() => Description); }
            set { SetProperty(() => Description, value); }
        }

        public string Phone
        {
            get { return GetProperty(() => Phone); }
            set { SetProperty(() => Phone, value); }
        }

        public string TotalMaxWeightAllowed
        {
            get { return GetProperty(() => TotalMaxWeightAllowed); }
            set { SetProperty(() => TotalMaxWeightAllowed, value); }
        }

        public double PlaceMaxWeightAllowed
        {
            get { return GetProperty(() => PlaceMaxWeightAllowed); }
            set { SetProperty(() => PlaceMaxWeightAllowed, value); }
        }
    }
}