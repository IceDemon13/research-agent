using System.Collections.ObjectModel;
using System.ComponentModel;
using DevExpress.Mvvm;
using DevExpress.Mvvm.DataAnnotations;
using Telemart.Client.Common.MvvmEnhancements;
using Telemart.Client.Core.Helpers;
using Telemart.Client.Helpers;
using Telemart.Client.Properties;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.BankPayment;
using Telemart.Client.ViewModels.Validation;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.ViewModels.Money.Receive.ImportBankPayments
{
    public sealed class ImportBankPaymentsModel : BindableBase, IDataErrorInfo
    {
        public const int Ok = 1;
        public const int WaitsOtpConfirmation = 2;
        public const int WaitsPhoneToSendOtp = 3;

        public int CashboxId
        {
            get { return GetProperty(() => CashboxId); }
            set { SetProperty(() => CashboxId, value); }
        }

        public string SessionId
        {
            get { return GetProperty(() => SessionId); }
            set { SetProperty(() => SessionId, value); }
        }

        public int SessionStateId
        {
            get { return GetProperty(() => SessionStateId); }
            set { SetProperty(() => SessionStateId, value, () => { RaisePropertiesChanged(nameof(OtpDev), nameof(Otp)); }); }
        }

        public ObservableCollection<ComboBoxItem> Phones
        {
            get { return GetProperty(() => Phones); }
            set { SetProperty(() => Phones, value); }
        }

        public int OtpDev
        {
            get { return GetProperty(() => OtpDev); }
            set { SetProperty(() => OtpDev, value); }
        }

        public string Otp
        {
            get { return GetProperty(() => Otp); }
            set { SetProperty(() => Otp, value); }
        }

        public ObservableCollection<ValidationResultItem> ValidationItems
        {
            get { return GetProperty(() => ValidationItems); }
            set { SetProperty(() => ValidationItems, value); }
        }

        public Result<BankPaymentDto[]> Result
        {
            get { return GetProperty(() => Result); }
            set { SetProperty(() => Result, value, () => { RaisePropertyChanged(nameof(ResultString)); }); }
        }

        public string ResultString => Result != null
            ? $"{GetAction(Result.Data.Length)} {Result.Data.Length} {GetQuantity(Result.Data.Length)}"
            : string.Empty;

        #region IDataErrorInfo

        string IDataErrorInfo.Error => string.Empty;

        string IDataErrorInfo.this[string columnName] => IDataErrorInfoHelper.GetErrorText(this, columnName);

        #endregion

        public static void BuildMetadata(MetadataBuilder<ImportBankPaymentsModel> builder)
        {
            builder.Property(x => x.CashboxId).Required(() => Resources.RequiredErrorMessage);

            builder.Property(x => x.OtpDev).MatchesInstanceRule(
                (x, y) => y.SessionStateId != WaitsPhoneToSendOtp || x > 0,
                () => Resources.RequiredErrorMessage);

            builder.Property(x => x.Otp).MatchesInstanceRule(
                (x, y) => y.SessionStateId == Ok || !string.IsNullOrWhiteSpace(x),
                () => Resources.RequiredErrorMessage);
        }

        private static string GetAction(int n)
        {
            return n > 0
                ? WordEndingHelper.GetWordByNumber(n, new[] { "Импортированa", "Импортировано", "Импортировано" })
                : "Импортировано";
        }

        private static string GetQuantity(int n)
        {
            return n > 0
                ? WordEndingHelper.GetWordByNumber(n, new[] { "запись", "записи", "записей" })
                : "записей";
        }
    }
}