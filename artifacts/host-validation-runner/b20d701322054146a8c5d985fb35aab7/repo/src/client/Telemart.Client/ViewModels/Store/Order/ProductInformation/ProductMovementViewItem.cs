using System;
using DevExpress.Mvvm;

namespace Telemart.Client.ViewModels.Store.Order.ProductInformation
{
    public class ProductMovementViewItem : BindableBase
    {
        public string MovementName
        {
            get { return GetProperty(() => MovementName); }
            set { SetProperty(() => MovementName, value); }
        }

        public int Available
        {
            get { return GetProperty(() => Available); }
            set { SetProperty(() => Available, value); }
        }

        public int Overall
        {
            get { return GetProperty(() => Overall); }
            set { SetProperty(() => Overall, value); }
        }

        public DateTime DateIn
        {
            get { return GetProperty(() => DateIn); }
            set { SetProperty(() => DateIn, value); }
        }
    }
}