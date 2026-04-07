using System;
using DevExpress.Mvvm.DataAnnotations;
using Telemart.Client.Dictionaries;
using Telemart.Client.Properties;
using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.Store.Order
{
    public class OrderGeneralConfirmSettingViewItem : TelemartCloneableViewItemBase
    {
        private Func<OrderGeneralConfirmSettingViewItem, bool> _validPredicate;
        private string _errorMessage;

        public int Id
        {
            get { return GetProperty(() => Id); }
            set { SetProperty(() => Id, value); }
        }

        public string DisplayName
        {
            get { return GetProperty(() => DisplayName); }
            set { SetProperty(() => DisplayName, value); }
        }

        public string Description
        {
            get { return GetProperty(() => Description); }
            set { SetProperty(() => Description, value); }
        }

        public string Value
        {
            get { return GetProperty(() => Value); }
            set { SetProperty(() => Value, value); }
        }

        public string ValueOld
        {
            get { return GetProperty(() => ValueOld); }
            set { SetProperty(() => ValueOld, value, ChangeValueOld); }
        }

        public bool Active
        {
            get { return GetProperty(() => Active); }
            set { SetProperty(() => Active, value, () => RaisePropertiesChanged(nameof(IntValue))); }
        }

        public bool ActiveOld
        {
            get { return GetProperty(() => ActiveOld); }
            set { SetProperty(() => ActiveOld, value); }
        }

        public int? IntValue
        {
            get { return GetProperty(() => IntValue); }
            set { SetProperty(() => IntValue, value, ChangeIntValue); }
        }

        public bool BoolValue
        {
            get { return GetProperty(() => BoolValue); }
            set { SetProperty(() => BoolValue, value, ChangeBoolValue); }
        }

        public static void BuildMetadata(MetadataBuilder<OrderGeneralConfirmSettingViewItem> builder)
        {
            builder.Property(x => x.IntValue)
                .MatchesInstanceRule((x, y) => !y.Active || !int.TryParse(y.ValueOld, out int _) || y.IsValid, (x, y) => y.ErrorMessage);
        }

        public bool IsChanged()
        {
            return Active != ActiveOld || ValueOld != Value;
        }

        public void SetValid(Func<OrderGeneralConfirmSettingViewItem, bool> validPredicate, string errorMessage)
        {
            _validPredicate = validPredicate;
            _errorMessage = errorMessage;
        }

        public bool IsValid => _validPredicate?.Invoke(this) ?? true;

        public string ErrorMessage => _errorMessage;

        private void ChangeValueOld()
        {
            if ((Id == OrderConfirmConstants.SalesHistoryDaysSettingId || Id == OrderConfirmConstants.SalesHistoryDeviationSettingId) && int.TryParse(Value, out int intValue))
            {
                IntValue = intValue;
                RaisePropertyChanged(nameof(IntValue));
            }

            if ((Id == OrderConfirmConstants.AllowableOnlyDontCallId || Id == OrderConfirmConstants.AllowSalesHistoryDeviationSettingId) && bool.TryParse(Value, out bool boolValue))
            {
                BoolValue = boolValue;
                RaisePropertyChanged(nameof(BoolValue));
            }
        }

        private void ChangeIntValue()
        {
            Value = IntValue?.ToString();
        }

        private void ChangeBoolValue()
        {
            Value = BoolValue.ToString();
        }
    }
}