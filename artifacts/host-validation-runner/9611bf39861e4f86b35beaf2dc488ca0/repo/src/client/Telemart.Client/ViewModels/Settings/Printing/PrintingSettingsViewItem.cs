using System.ComponentModel;
using System.Linq;
using DevExpress.Mvvm;
using DevExpress.Mvvm.DataAnnotations;
using Telemart.Client.Dictionaries;
using Telemart.Client.Properties;

namespace Telemart.Client.ViewModels.Settings.Printing
{
    public class PrintingSettingsViewItem : BindableBase, IDataErrorInfo
    {
        public PrintingSettingsType Type
        {
            get { return GetProperty(() => Type); }
            set { SetProperty(() => Type, value); }
        }

        public PrinterInfo Printer
        {
            get { return GetProperty(() => Printer); }
            set { SetProperty(() => Printer, value, () => PaperSource = Printer?.PaperSources.Count == 1 ? Printer.PaperSources.First() : null); }
        }

        public string PaperSource
        {
            get { return GetProperty(() => PaperSource); }
            set { SetProperty(() => PaperSource, value); }
        }

        public bool Required
        {
            get { return GetProperty(() => Required); }
            set { SetProperty(() => Required, value, () => RaisePropertyChanged(nameof(Printer))); }
        }

        string IDataErrorInfo.Error => string.Empty;

        string IDataErrorInfo.this[string columnName] => IDataErrorInfoHelper.GetErrorText(this, columnName);

        public static void BuildMetadata(MetadataBuilder<PrintingSettingsViewItem> builder)
        {
            builder.Property(x => x.PaperSource)
                .MatchesInstanceRule(
                    (x, y) => y.Printer == null || x != null || !y.Printer.PaperSources.Any(),
                    () => Resources.RequiredErrorMessage);
            builder.Property(x => x.Printer)
                .MatchesInstanceRule(
                    (x, y) => !y.Required || x != null,
                    () => Resources.RequiredErrorMessage);
        }
    }
}
