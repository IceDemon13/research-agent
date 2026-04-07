using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using DevExpress.Mvvm;
using DevExpress.Mvvm.DataAnnotations;
using DevExpress.Mvvm.Native;
using Telemart.Client.Business.Delivery.ScanSheets;
using Telemart.Client.Common.MvvmEnhancements;
using Telemart.Client.Dictionaries;
using Telemart.Client.TransferObjects;
using Telemart.Client.ViewModels.Validation;

namespace Telemart.Client.ViewModels.Store.CreateScanSheet
{
    public sealed class CreateScanSheetModel : BindableBase, IDataErrorInfo
    {
        private readonly IDictionaries dictionaries;

        private readonly HashSet<int> availableCarryIds;
        private readonly HashSet<int> warehouseDeliverriesCarryIds;

        public CreateScanSheetModel(IDictionaries dictionaries, HashSet<int> availableCarryIds, HashSet<int> warehouseDeliverriesCarryIds)
        {
            this.dictionaries = dictionaries;
            this.availableCarryIds = availableCarryIds;
            this.warehouseDeliverriesCarryIds = warehouseDeliverriesCarryIds;
        }

        public ScanSheetProcessorType? ScanSheetProcessorType
        {
            get { return GetProperty(() => ScanSheetProcessorType); }
            set { SetProperty(() => ScanSheetProcessorType, value, ScanSheetProcessorTypeChangedCallback); }
        }

        public ReadOnlyObservableCollection<ComboBoxItem> Carries
        {
            get { return GetProperty(() => Carries); }
            private set { SetProperty(() => Carries, value); }
        }

        public ObservableCollection<ComboBoxItem> SelectedCarries
        {
            get { return GetProperty(() => SelectedCarries); }
            set { SetProperty(() => SelectedCarries, value); }
        }

        public ComboBoxItem SelectedState
        {
            get { return GetProperty(() => SelectedState); }
            set { SetProperty(() => SelectedState, value); }
        }

        public DateTime? DateFrom
        {
            get { return GetProperty(() => DateFrom); }
            set { SetProperty(() => DateFrom, value); }
        }

        public DateTime? DateTo
        {
            get { return GetProperty(() => DateTo); }
            set { SetProperty(() => DateTo, value); }
        }

        public ComboBoxItem SelectedWarehouse
        {
            get { return GetProperty(() => SelectedWarehouse); }
            set { SetProperty(() => SelectedWarehouse, value); }
        }

        public ObservableCollection<ScanSheetOrderViewItem> Orders
        {
            get { return GetProperty(() => Orders); }
            set { SetProperty(() => Orders, value); }
        }

        public ScanSheetOrderViewItem SelectedOrder
        {
            get { return GetProperty(() => SelectedOrder); }
            set { SetProperty(() => SelectedOrder, value); }
        }

        public int PlacesCount
        {
            get { return GetProperty(() => PlacesCount); }
            set { SetProperty(() => PlacesCount, value); }
        }

        public ObservableCollection<ValidationResultItem> ValidationItems
        {
            get { return GetProperty(() => ValidationItems); }
            set { SetProperty(() => ValidationItems, value); }
        }

        public ScanSheetCreateResponse[] Result
        {
            get { return GetProperty(() => Result); }
            set { SetProperty(() => Result, value); }
        }

        public int ProgressValue
        {
            get { return GetProperty(() => ProgressValue); }
            set { SetProperty(() => ProgressValue, value); }
        }

        public string ProgressText
        {
            get { return GetProperty(() => ProgressText); }
            set { SetProperty(() => ProgressText, value); }
        }

        public ComboBoxItem? SelectedCourierCall
        {
            get { return GetProperty(() => SelectedCourierCall); }
            set { SetProperty(() => SelectedCourierCall, value); }
        }

        public bool CompleteCourierCall
        {
            get { return GetProperty(() => CompleteCourierCall); }
            set { SetProperty(() => CompleteCourierCall, value); }
        }

        #region IDataErrorInfo

        string IDataErrorInfo.Error => string.Empty;

        string IDataErrorInfo.this[string columnName] => IDataErrorInfoHelper.GetErrorText(this, columnName);

        #endregion

        public static void BuildMetadata(MetadataBuilder<CreateScanSheetModel> builder)
        {
            builder.Property(x => x.ScanSheetProcessorType)
                .MatchesRule(x => x.HasValue, () => "Выберите тип реестра");

            builder.Property(x => x.SelectedCarries)
                .MatchesRule(x => x != null && x.Any(), () => "Выберите способ доставки");
        }

        private static IEnumerable<int> GetCarries(ScanSheetProcessorType scanSheetProcessorType)
        {
            switch (scanSheetProcessorType)
            {
                case Business.Delivery.ScanSheets.ScanSheetProcessorType.Novaposhta:
                    yield return CarryType.NpWarehouseId;
                    yield return CarryType.NpDeliveryId;
                    yield return CarryType.NpPostBoxId;
                    yield return CarryType.LocalExpressId;
                    break;
                case Business.Delivery.ScanSheets.ScanSheetProcessorType.Telemart:
                    yield return CarryType.DeliveryId;
                    yield return CarryType.KievDeliveryId;
                    yield return CarryType.HomenkoId;
                    yield return CarryType.SmartPostId;
                    yield return CarryType.GabaritkaId;
                    yield return CarryType.TelemartServiceCourierId;
                    break;
                case Business.Delivery.ScanSheets.ScanSheetProcessorType.MeestExpress:
                    yield return CarryType.MeWarehouseId;
                    yield return CarryType.MeDeliveryId;
                    yield return CarryType.MeMiniWarehouseId;
                    yield return CarryType.MePostBoxId;
                    break;
                case Business.Delivery.ScanSheets.ScanSheetProcessorType.Ukrposhta:
                    yield return CarryType.UpDeliveryId;
                    yield return CarryType.UpWarehouseId;
                    break;
                default:
                    throw new NotSupportedException();
            }
        }

        private void ScanSheetProcessorTypeChangedCallback()
        {
            if (ScanSheetProcessorType.HasValue)
            {
                Carries = GetCarries(ScanSheetProcessorType.Value)
                    .Select(x => dictionaries.GetItemById<CarryType>(x))
                    .Where(x => x != null && ((warehouseDeliverriesCarryIds.Contains(x.Id) && x.Active) || availableCarryIds.Contains(x.Id)))
                    .OrderBy(x => x.Id)
                    .Select(x => new ComboBoxItem(x.Id, x.Name))
                    .ToReadOnlyObservableCollection();
                SelectedCarries = Carries.ToObservableCollection();
            }
            else
            {
                Carries = null;
                SelectedCarries = null;
            }
        }
    }
}