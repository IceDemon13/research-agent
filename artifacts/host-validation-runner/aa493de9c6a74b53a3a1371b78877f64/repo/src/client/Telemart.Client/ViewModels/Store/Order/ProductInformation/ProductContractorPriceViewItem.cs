using System;
using DevExpress.Mvvm.DataAnnotations;
using DevExpress.Mvvm.POCO;

namespace Telemart.Client.ViewModels.Store.Order.ProductInformation
{
    [POCOViewModel]
    public class ProductContractorPriceViewItem : ILinkSupport
    {
        protected ProductContractorPriceViewItem()
        {
        }

        public virtual int ContractorId { get; set; }

        public virtual string ContractorName { get; set; }

        public virtual decimal PriceUah { get; set; }

        public virtual decimal SourcePriceUah { get; set; }

        public virtual decimal PriceUsd { get; set; }

        public virtual decimal SourcePriceUsd { get; set; }

        public virtual string DisplayPriceToolTip { get; set; }

        public virtual bool Consider { get; set; }

        public virtual string Avail { get; set; }

        public virtual DateTime DateAdd { get; set; }

        public virtual string Link { get; set; }

        public virtual decimal? ExtraCharge { get; set; }

        public virtual string DisplayPrice { get; set; }

        public virtual bool IsHighestExtraCharge { get; set; }

        public virtual bool IsSecondHighestExtraCharge { get; set; }

        public virtual bool IsLowestPrice { get; set; }

        public virtual bool IsSecondLowestPrice { get; set; }

        public static ProductContractorPriceViewItem Create()
        {
            return ViewModelSource<ProductContractorPriceViewItem>.Create();
        }
    }
}