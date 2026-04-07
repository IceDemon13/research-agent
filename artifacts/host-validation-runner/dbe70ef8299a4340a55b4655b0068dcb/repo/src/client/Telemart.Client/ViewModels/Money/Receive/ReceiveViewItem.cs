using System;
using System.ComponentModel;
using DevExpress.Mvvm;
using DevExpress.Mvvm.DataAnnotations;
using Telemart.Client.Core.Cloning;
using Telemart.Client.Dictionaries;
using Telemart.Client.Properties;

namespace Telemart.Client.ViewModels.Money.Receive
{
    public sealed class ReceiveViewItem : BindableBase, IDataErrorInfo, ICloneable
    {
        public ReceiveViewItem(int number, string raw)
        {
            Number = number;
            Raw = raw.Trim();
        }

        public ReceiveViewItem()
        {
        }

        public int Number
        {
            get { return GetProperty(() => Number); }
            set { SetProperty(() => Number, value); }
        }

        public string Raw
        {
            get { return GetProperty(() => Raw); }
            set { SetProperty(() => Raw, value); }
        }

        public int OrderId
        {
            get { return GetProperty(() => OrderId); }
            set { SetProperty(() => OrderId, value); }
        }

        public string TrackNumber
        {
            get { return GetProperty(() => TrackNumber); }
            set { SetProperty(() => TrackNumber, value); }
        }

        public decimal PlannedAmount
        {
            get { return GetProperty(() => PlannedAmount); }
            set { SetProperty(() => PlannedAmount, value, () => { RaisePropertyChanged(nameof(ActualAmount)); }); }
        }

        public decimal ActualAmount
        {
            get { return GetProperty(() => ActualAmount); }
            set { SetProperty(() => ActualAmount, value); }
        }

        public OrderStatus OrderState
        {
            get { return GetProperty(() => OrderState); }
            set { SetProperty(() => OrderState, value); }
        }

        public bool Processed
        {
            get { return GetProperty(() => Processed); }
            set { SetProperty(() => Processed, value); }
        }

        public bool Recognized
        {
            get { return GetProperty(() => Recognized); }
            set { SetProperty(() => Recognized, value); }
        }

        public bool Recognizing
        {
            get { return GetProperty(() => Recognizing); }
            set { SetProperty(() => Recognizing, value); }
        }

        public decimal? CodCommission
        {
            get { return GetProperty(() => CodCommission); }
            set { SetProperty(() => CodCommission, value); }
        }

        public DateTime? Received
        {
            get { return GetProperty(() => Received); }
            set { SetProperty(() => Received, value); }
        }

        #region IDataErrorInfo

        string IDataErrorInfo.Error => string.Empty;

        string IDataErrorInfo.this[string columnName] => IDataErrorInfoHelper.GetErrorText(this, columnName);

        #endregion

        public static void BuildMetadata(MetadataBuilder<ReceiveViewItem> builder)
        {
            builder.Property(x => x.ActualAmount).MatchesRule(x => x > 0, () => Resources.RequiredErrorMessage);
        }

        object ICloneable.Clone()
        {
            return Clone();
        }

        public ReceiveViewItem Clone()
        {
            return ReflectionObjectCloner.Clone(this);
        }
    }
}