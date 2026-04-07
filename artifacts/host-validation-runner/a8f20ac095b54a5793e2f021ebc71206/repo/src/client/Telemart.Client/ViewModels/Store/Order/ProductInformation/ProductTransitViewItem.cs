using System;
using DevExpress.Mvvm;

namespace Telemart.Client.ViewModels.Store.Order.ProductInformation
{
    public class ProductTransitViewItem : BindableBase
    {
        public string TransitName
        {
            get { return GetProperty(() => TransitName); }
            set { SetProperty(() => TransitName, value); }
        }

        public int Available
        {
            get { return GetProperty(() => Available); }
            set { SetProperty(() => Available, value); }
        }

        public bool IgnoreTransit
        {
            get { return GetProperty(() => IgnoreTransit); }
            set { SetProperty(() => IgnoreTransit, value); }
        }

        public int Overall
        {
            get { return GetProperty(() => Overall); }
            set { SetProperty(() => Overall, value); }
        }

        public DateTime DateGet
        {
            get { return GetProperty(() => DateGet); }
            set { SetProperty(() => DateGet, value); }
        }
    }
}