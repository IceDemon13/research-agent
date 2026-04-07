using System;
using System.Collections.ObjectModel;
using DevExpress.Mvvm;

namespace Telemart.Client.ViewModels.Store.Order.PackList
{
    public class PackListViewItem : BindableBase
    {
        public int Id
        {
            get { return GetProperty(() => Id); }
            set { SetProperty(() => Id, value); }
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

        public DateTime? CompletedOn
        {
            get { return GetProperty(() => CompletedOn); }
            set { SetProperty(() => CompletedOn, value); }
        }

        public int? CompletedBy
        {
            get { return GetProperty(() => CompletedBy); }
            set { SetProperty(() => CompletedBy, value); }
        }

        public DateTime? CollectedOn
        {
            get { return GetProperty(() => CollectedOn); }
            set { SetProperty(() => CollectedOn, value, () => RecognizeBarcodeViewModelVisible = CollectedOn.HasValue); }
        }

        public bool RecognizeBarcodeViewModelVisible
        {
            get { return GetProperty(() => RecognizeBarcodeViewModelVisible); }
            private set { SetProperty(() => RecognizeBarcodeViewModelVisible, value); }
        }

        public int? CollectedBy
        {
            get { return GetProperty(() => CollectedBy); }
            set { SetProperty(() => CollectedBy, value); }
        }

        public int? PackagerEmployeeId
        {
            get { return GetProperty(() => PackagerEmployeeId); }
            set { SetProperty(() => PackagerEmployeeId, value); }
        }

        public int? CollectorEmployeeId
        {
            get { return GetProperty(() => CollectorEmployeeId); }
            set { SetProperty(() => CollectorEmployeeId, value); }
        }

        public ObservableCollection<PackListOrderViewItem> PackListOrders
        {
            get { return GetProperty(() => PackListOrders); }
            set { SetProperty(() => PackListOrders, value); }
        }
    }
}