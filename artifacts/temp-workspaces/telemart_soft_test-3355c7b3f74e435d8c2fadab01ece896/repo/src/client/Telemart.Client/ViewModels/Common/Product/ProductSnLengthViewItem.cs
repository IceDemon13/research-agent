using System;
using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.Common.Product
{
    public class ProductSnLengthViewItem : TelemartViewItemBase
    {
        public int Id
        {
            get { return GetProperty(() => Id); }
            set { SetProperty(() => Id, value); }
        }

        public int ProductId
        {
            get { return GetProperty(() => ProductId); }
            set { SetProperty(() => ProductId, value); }
        }

        public int Length
        {
            get { return GetProperty(() => Length); }
            set { SetProperty(() => Length, value); }
        }

        public DateTime? CreatedOn
        {
            get { return GetProperty(() => CreatedOn); }
            set { SetProperty(() => CreatedOn, value); }
        }

        public int? CreatedBy
        {
            get { return GetProperty(() => CreatedBy); }
            set { SetProperty(() => CreatedBy, value); }
        }
    }
}