using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using DevExpress.Mvvm;
using DevExpress.Mvvm.DataAnnotations;
using DevExpress.Mvvm.Native;
using Telemart.Client.Core.Cloning;
using Telemart.Client.Dictionaries;
using Telemart.Client.Properties;
using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.SupplierBill
{
    public sealed class SupplierBillViewItem : BindableBase, ILockableEntity, IDataErrorInfo, ICloneable
    {
        public int Id
        {
            get { return GetProperty(() => Id); }
            set { SetProperty(() => Id, value); }
        }

        public int SupplierId
        {
            get { return GetProperty(() => SupplierId); }
            set { SetProperty(() => SupplierId, value); }
        }

        public int CurrencyId
        {
            get { return GetProperty(() => CurrencyId); }
            set { SetProperty(() => CurrencyId, value, () => RaisePropertyChanged(nameof(Tax))); }
        }

        public int? InvoiceCarryId
        {
            get { return GetProperty(() => InvoiceCarryId); }
            set { SetProperty(() => InvoiceCarryId, value); }
        }

        public int StateId
        {
            get { return GetProperty(() => StateId); }
            set { SetProperty(() => StateId, value); }
        }

        public int? EmployeeLockId
        {
            get { return GetProperty(() => EmployeeLockId); }
            set { SetProperty(() => EmployeeLockId, value); }
        }

        public string Edrpou
        {
            get { return GetProperty(() => Edrpou); }
            set { SetProperty(() => Edrpou, value); }
        }

        public string Number
        {
            get { return GetProperty(() => Number); }
            set { SetProperty(() => Number, value); }
        }

        public DateTime InvoicedOn
        {
            get { return GetProperty(() => InvoicedOn); }
            set { SetProperty(() => InvoicedOn, value); }
        }

        public int? InvoiceId
        {
            get { return GetProperty(() => InvoiceId); }
            set { SetProperty(() => InvoiceId, value); }
        }

        public string Comment
        {
            get { return GetProperty(() => Comment); }
            set { SetProperty(() => Comment, value); }
        }

        public DateTime ModifiedOn
        {
            get { return GetProperty(() => ModifiedOn); }
            set { SetProperty(() => ModifiedOn, value); }
        }

        public int ModifiedBy
        {
            get { return GetProperty(() => ModifiedBy); }
            set { SetProperty(() => ModifiedBy, value); }
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

        public string EmployeeLockName
        {
            get { return GetProperty(() => EmployeeLockName); }
            set { SetProperty(() => EmployeeLockName, value); }
        }

        public ObservableCollection<SupplierBillProductViewItem> Products
        {
            get { return GetProperty(() => Products); }
            set { SetProperty(() => Products, value); }
        }

        public ObservableCollection<SupplierBillDocumentViewItem> Documents
        {
            get { return GetProperty(() => Documents); }
            set { SetProperty(() => Documents, value); }
        }

        public bool Tax => CurrencyId == Currency.UahId;

        public bool AnyBillDocuments => Documents?.Any(x => x.TypeId == SupplierBillDocumentType.Bill.Id) ?? false;

        public bool AnyInvoiceDocuments => Documents?.Any(x => x.TypeId == SupplierBillDocumentType.Invoice.Id) ?? false;

        public decimal? TotalPriceTax => Products?.Select(x => x.SumTax).DefaultIfEmpty(0).Sum();

        public decimal? TotalPrice => Products?.Select(x => x.Sum).DefaultIfEmpty(0).Sum();

        string IDataErrorInfo.Error => string.Empty;

        string IDataErrorInfo.this[string columnName] => IDataErrorInfoHelper.GetErrorText(this, columnName);

        public static void BuildMetadata(MetadataBuilder<SupplierBillViewItem> builder)
        {
            builder.Property(x => x.Number).Required(() => Resources.RequiredErrorMessage);
        }

        public object Clone()
        {
            SupplierBillViewItem item = ReflectionObjectCloner.Clone(this);

            item.Products = Products.Select(x => ReflectionObjectCloner.Clone(x)).ToObservableCollection();
            item.Documents = Documents.Select(x => ReflectionObjectCloner.Clone(x)).ToObservableCollection();

            return item;
        }
    }
}