using System.ComponentModel;
using DevExpress.Mvvm;
using DevExpress.Mvvm.DataAnnotations;
using Telemart.Client.Properties;

namespace Telemart.Client.ViewModels.Directories.Contractor.ParserSettings
{
    public class ParserSettingsCategoryReplaceViewItem : BindableBase, IDataErrorInfo
    {
        public string From
        {
            get { return GetProperty(() => From); }
            set { SetProperty(() => From, value); }
        }

        public string To
        {
            get { return GetProperty(() => To); }
            set { SetProperty(() => To, value); }
        }

        string IDataErrorInfo.Error => string.Empty;

        string IDataErrorInfo.this[string columnName] => IDataErrorInfoHelper.GetErrorText(this, columnName);

        public static void BuildMetadata(MetadataBuilder<ParserSettingsCategoryReplaceViewItem> builder)
        {
            builder.Property(x => x.From)
                .Required(() => Resources.RequiredErrorMessage);
        }
    }
}
