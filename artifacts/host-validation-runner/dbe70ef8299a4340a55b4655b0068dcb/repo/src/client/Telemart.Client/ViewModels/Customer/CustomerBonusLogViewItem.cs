using System;
using Newtonsoft.Json.Linq;
using Telemart.Client.Dictionaries;
using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.Customer
{
    public class CustomerBonusLogViewItem : TelemartViewItemBase
    {
        public int Id
        {
            get { return GetProperty(() => Id); }
            set { SetProperty(() => Id, value); }
        }

        public int BonusTypeId
        {
            get { return GetProperty(() => BonusTypeId); }
            set { SetProperty(() => BonusTypeId, value); }
        }

        public BonusType BonusType
        {
            get { return GetProperty(() => BonusType); }
            set { SetProperty(() => BonusType, value); }
        }

        public int TypeId
        {
            get { return GetProperty(() => TypeId); }
            set { SetProperty(() => TypeId, value); }
        }

        public int? DocumentId
        {
            get { return GetProperty(() => DocumentId); }
            set { SetProperty(() => DocumentId, value); }
        }

        public JObject Data
        {
            get { return GetProperty(() => Data); }
            set { SetProperty(() => Data, value); }
        }

        public string Quantity
        {
            get { return GetProperty(() => Quantity); }
            set { SetProperty(() => Quantity, value); }
        }

        public DateTime? CreatedOn
        {
            get { return GetProperty(() => CreatedOn); }
            set { SetProperty(() => CreatedOn, value); }
        }
    }
}