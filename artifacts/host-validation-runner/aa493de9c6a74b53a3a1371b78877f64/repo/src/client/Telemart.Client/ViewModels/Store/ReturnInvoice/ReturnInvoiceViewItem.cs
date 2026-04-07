using System;
using System.Collections.Generic;
using System.Linq;
using Telemart.Client.Core.Cloning;
using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.Store.ReturnInvoice
{
    public class ReturnInvoiceViewItem : TelemartEditorViewItemBase
    {
        public int InvoiceId
        {
            get { return GetProperty(() => InvoiceId); }
            set { SetProperty(() => InvoiceId, value); }
        }

        public int WarehouseId
        {
            get { return GetProperty(() => WarehouseId); }
            set { SetProperty(() => WarehouseId, value); }
        }

        public int SupplierId
        {
            get { return GetProperty(() => SupplierId); }
            set { SetProperty(() => SupplierId, value); }
        }

        public string SupplierName
        {
            get { return GetProperty(() => SupplierName); }
            set { SetProperty(() => SupplierName, value); }
        }

        public bool DocumentsControl
        {
            get { return GetProperty(() => DocumentsControl); }
            set { SetProperty(() => DocumentsControl, value); }
        }

        public bool ReadyPack
        {
            get { return GetProperty(() => ReadyPack); }
            set { SetProperty(() => ReadyPack, value); }
        }

        public int CarryId
        {
            get { return GetProperty(() => CarryId); }
            set { SetProperty(() => CarryId, value); }
        }

        public int StateId
        {
            get { return GetProperty(() => StateId); }
            set { SetProperty(() => StateId, value); }
        }

        public int? DocumentsCount
        {
            get { return GetProperty(() => DocumentsCount); }
            set { SetProperty(() => DocumentsCount, value); }
        }

        public int? BitrixId
        {
            get { return GetProperty(() => BitrixId); }
            set { SetProperty(() => BitrixId, value); }
        }

        public bool? CreatedIn1C
        {
            get { return GetProperty(() => CreatedIn1C); }
            set { SetProperty(() => CreatedIn1C, value); }
        }

        public DateTime? ReturnDate
        {
            get { return GetProperty(() => ReturnDate); }
            set { SetProperty(() => ReturnDate, value); }
        }

        public DateTime? ReturnedOn
        {
            get { return GetProperty(() => ReturnedOn); }
            set { SetProperty(() => ReturnedOn, value); }
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

        public string SenderNpContractorRef
        {
            get { return GetProperty(() => SenderNpContractorRef); }
            set { SetProperty(() => SenderNpContractorRef, value); }
        }

        public string TrackNumber
        {
            get { return GetProperty(() => TrackNumber); }
            set { SetProperty(() => TrackNumber, value); }
        }

        public List<ReturnInvoiceProductViewItem> Products
        {
            get { return GetProperty(() => Products); }
            set { SetProperty(() => Products, value); }
        }

        public override object Clone()
        {
            ReturnInvoiceViewItem item = ReflectionObjectCloner.Clone(this);

            item.Products = Products.Select(x =>
            {
                ReturnInvoiceProductViewItem viewItem = ReflectionObjectCloner.Clone(x, () => new ReturnInvoiceProductViewItem(x.OldPrice));
                viewItem.SerialNumbers = x.SerialNumbers.ToList();
                viewItem.OutQuantity = x.OutQuantity;
                return viewItem;
            }).ToList();

            return item;
        }
    }
}