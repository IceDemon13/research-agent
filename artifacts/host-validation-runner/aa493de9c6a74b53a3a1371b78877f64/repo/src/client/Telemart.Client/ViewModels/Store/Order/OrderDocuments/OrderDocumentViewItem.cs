using System;
using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.Store.Order.OrderDocuments
{
    public sealed class OrderDocumentViewItem : TelemartViewItemBase
    {
        public int Id
        {
            get { return GetProperty(() => Id); }
            set { SetProperty(() => Id, value); }
        }

        public string Name
        {
            get { return GetProperty(() => Name); }
            set { SetProperty(() => Name, value); }
        }

        public string Ext
        {
            get { return GetProperty(() => Ext); }
            set { SetProperty(() => Ext, value); }
        }

        public int TypeId
        {
            get { return GetProperty(() => TypeId); }
            set { SetProperty(() => TypeId, value); }
        }

        public int OrderId
        {
            get { return GetProperty(() => OrderId); }
            set { SetProperty(() => OrderId, value); }
        }

        public DateTime CreatedOn
        {
            get { return GetProperty(() => CreatedOn); }
            set { SetProperty(() => CreatedOn, value); }
        }

        public int CreatedBy
        {
            get { return GetProperty(() => CreatedBy); }
            set { SetProperty(() => CreatedBy, value); }
        }

        public byte[] Data
        {
            get { return GetProperty(() => Data); }
            set { SetProperty(() => Data, value); }
        }
    }
}