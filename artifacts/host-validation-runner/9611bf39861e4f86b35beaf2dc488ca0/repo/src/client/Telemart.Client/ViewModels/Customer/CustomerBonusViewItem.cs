using System;
using System.Collections.Generic;
using System.Text;
using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.Customer
{
    public class CustomerBonusViewItem : TelemartViewItemBase
    {
        public int Id
        {
            get { return GetProperty(() => Id); }
            set { SetProperty(() => Id, value); }
        }

        public int CustomerId
        {
            get { return GetProperty(() => CustomerId); }
            set { SetProperty(() => CustomerId, value); }
        }

        public int BonusTypeId
        {
            get { return GetProperty(() => BonusTypeId); }
            set { SetProperty(() => BonusTypeId, value); }
        }

        public string BonusTypeName
        {
            get { return GetProperty(() => BonusTypeName); }
            set { SetProperty(() => BonusTypeName, value); }
        }

        public int Quantity
        {
            get { return GetProperty(() => Quantity); }
            set { SetProperty(() => Quantity, value); }
        }
    }
}
