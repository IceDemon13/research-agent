using System.ComponentModel;
using System.Text.RegularExpressions;
using DevExpress.Mvvm;
using DevExpress.Mvvm.DataAnnotations;
using Telemart.Client.Dictionaries;

namespace Telemart.Client.ViewModels
{
    public sealed class InvoiceTtnViewItem : BindableBase, IDataErrorInfo
    {
        public InvoiceTtnViewItem(CarryType carryType)
        {
            CarryType = carryType;
        }

        public int Id
        {
            get { return GetProperty(() => Id); }
            set { SetProperty(() => Id, value); }
        }

        public string Ttn
        {
            get { return GetProperty(() => Ttn); }
            set { SetProperty(() => Ttn, value); }
        }

        public CarryType CarryType { get; }

        string IDataErrorInfo.Error => string.Empty;

        string IDataErrorInfo.this[string columnName] => IDataErrorInfoHelper.GetErrorText(this, columnName);

        public static void BuildMetadata(MetadataBuilder<InvoiceTtnViewItem> builder)
        {
            builder.Property(x => x.Ttn)
                .MatchesInstanceRule(
                    (x, y) => string.IsNullOrWhiteSpace(y.CarryType.TtnRegex) || string.IsNullOrWhiteSpace(x) || Regex.IsMatch(x, y.CarryType.TtnRegex),
                    () => "Введите корректно номер ТТН");
        }
    }
}
