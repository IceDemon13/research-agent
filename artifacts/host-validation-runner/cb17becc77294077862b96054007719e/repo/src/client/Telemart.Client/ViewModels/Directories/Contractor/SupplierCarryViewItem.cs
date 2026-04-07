using System;
using System.ComponentModel;
using DevExpress.Mvvm;
using DevExpress.Mvvm.DataAnnotations;
using Telemart.Client.Core.Cloning;
using Telemart.Client.Dictionaries;
using Telemart.Client.Properties;

namespace Telemart.Client.ViewModels.Directories.Contractor
{
    public sealed class SupplierCarryViewItem : BindableBase, IDataErrorInfo, ICloneable
    {
        public CarryType CarryType
        {
            get { return GetProperty(() => CarryType); }
            set { SetProperty(() => CarryType, value); }
        }

        public int WarehouseId
        {
            get { return GetProperty(() => WarehouseId); }
            set { SetProperty(() => WarehouseId, value); }
        }

        public int? SupplierWarehouseId
        {
            get { return GetProperty(() => SupplierWarehouseId); }
            set { SetProperty(() => SupplierWarehouseId, value); }
        }

        public int Days
        {
            get { return GetProperty(() => Days); }
            set { SetProperty(() => Days, value); }
        }

        public string Filial
        {
            get { return GetProperty(() => Filial); }
            set { SetProperty(() => Filial, value); }
        }

        public int Id
        {
            get { return GetProperty(() => Id); }
            set { SetProperty(() => Id, value, () => RaisePropertyChanged(nameof(IdDisplayValue))); }
        }

        public TimeSpan TimeClose
        {
            get { return GetProperty(() => TimeClose); }
            set { SetProperty(() => TimeClose, value, () => RaisePropertyChanged(nameof(TimeCloseStr))); }
        }

        public TimeSpan TimeCloseSt
        {
            get { return GetProperty(() => TimeCloseSt); }
            set { SetProperty(() => TimeCloseSt, value, () => RaisePropertiesChanged(nameof(TimeCloseStr), nameof(TimeCloseSt), nameof(TimeArriveSt), nameof(TimeGetSt))); }
        }

        public string TimeCloseStr => FormatTime(TimeClose, TimeCloseSt);

        public TimeSpan TimeArrive
        {
            get { return GetProperty(() => TimeArrive); }
            set { SetProperty(() => TimeArrive, value, () => RaisePropertyChanged(nameof(TimeArriveStr))); }
        }

        public TimeSpan TimeArriveSt
        {
            get { return GetProperty(() => TimeArriveSt); }
            set { SetProperty(() => TimeArriveSt, value, () => RaisePropertiesChanged(nameof(TimeArriveStr), nameof(TimeCloseSt), nameof(TimeArriveSt), nameof(TimeGetSt))); }
        }

        public string TimeArriveStr => FormatTime(TimeArrive, TimeArriveSt);

        public TimeSpan TimeGet
        {
            get { return GetProperty(() => TimeGet); }
            set { SetProperty(() => TimeGet, value, () => RaisePropertyChanged(nameof(TimeGetStr))); }
        }

        public TimeSpan TimeGetSt
        {
            get { return GetProperty(() => TimeGetSt); }
            set { SetProperty(() => TimeGetSt, value, () => RaisePropertiesChanged(nameof(TimeGetStr), nameof(TimeCloseSt), nameof(TimeArriveSt), nameof(TimeGetSt))); }
        }

        public string TimeGetStr => FormatTime(TimeGet, TimeGetSt);

        public bool Main
        {
            get { return GetProperty(() => Main); }
            set { SetProperty(() => Main, value); }
        }

        public int? IdDisplayValue => Id > 0 ? Id : (int?)null;

        #region IDataErrorInfo

        string IDataErrorInfo.Error => string.Empty;

        string IDataErrorInfo.this[string columnName] => IDataErrorInfoHelper.GetErrorText(this, columnName);

        #endregion

        public static void BuildMetadata(MetadataBuilder<SupplierCarryViewItem> builder)
        {
            builder.Property(x => x.WarehouseId)
                .MatchesRule(id => id > 0, () => Resources.RequiredErrorMessage);

            builder.Property(x => x.SupplierWarehouseId)
                .Required(() => Resources.RequiredErrorMessage);

            builder.Property(x => x.CarryType)
                .Required(() => Resources.RequiredErrorMessage);

            builder.Property(x => x.Days)
                .MatchesRule(x => x >= 0 && x <= SupplierCarryConstants.DaysMaxValue);

            builder.Property(x => x.TimeClose)
                .MatchesRule(x => x != TimeSpan.Zero, () => Resources.RequiredErrorMessage)
                .Required(() => Resources.RequiredErrorMessage);

            builder.Property(x => x.TimeArrive)
                .MatchesRule(x => x != TimeSpan.Zero, () => Resources.RequiredErrorMessage)
                .Required(() => Resources.RequiredErrorMessage);

            builder.Property(x => x.TimeGet)
                .MatchesRule(x => x != TimeSpan.Zero, () => Resources.RequiredErrorMessage)
                .Required(() => Resources.RequiredErrorMessage);
        }

        public SupplierCarryViewItem Clone()
        {
            return ReflectionObjectCloner.Clone(this);
        }

        object ICloneable.Clone()
        {
            return Clone();
        }

        private static string FormatTime(TimeSpan time, TimeSpan timeSt)
        {
            const string TimeSpanFormat = @"hh\:mm";
            return $"{time.ToString(TimeSpanFormat)}{(timeSt == TimeSpan.Zero ? string.Empty : $"; {timeSt.ToString(TimeSpanFormat)}")}";
        }
    }
}