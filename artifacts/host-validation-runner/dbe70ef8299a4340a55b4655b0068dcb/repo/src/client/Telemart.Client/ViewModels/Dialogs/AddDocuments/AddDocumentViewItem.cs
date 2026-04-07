using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;
using DevExpress.Mvvm;
using DevExpress.Mvvm.DataAnnotations;
using Telemart.Client.Core.IO;
using Telemart.Client.Properties;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.ViewModels.Dialogs.AddDocuments
{
    public class AddDocumentViewItem : BindableBase, IDataErrorInfo
    {
        public AddDocumentViewItem(string filePath)
        {
            FilePath = filePath;
            FileName = Path.GetFileName(filePath);
        }

        public AddDocumentViewItem(DocumentDto document)
        {
            FileName = document.Name;
            Data = document.Data;
            Extension = document.Extension;
        }

        public byte[] Data
        {
            get { return GetProperty(() => Data); }
            private set { SetProperty(() => Data, value); }
        }

        public string FilePath
        {
            get { return GetProperty(() => FilePath); }
            private set { SetProperty(() => FilePath, value); }
        }

        public string Extension
        {
            get { return GetProperty(() => Extension); }
            private set { SetProperty(() => Extension, value); }
        }

        public string FileName
        {
            get { return GetProperty(() => FileName); }
            private set { SetProperty(() => FileName, value); }
        }

        public int? DocumentTypeId
        {
            get { return GetProperty(() => DocumentTypeId); }
            set { SetProperty(() => DocumentTypeId, value); }
        }

        public string DocumentName
        {
            get { return GetProperty(() => DocumentName); }
            set { SetProperty(() => DocumentName, value); }
        }

        public string ErrorMessage
        {
            get { return GetProperty(() => ErrorMessage); }
            set { SetProperty(() => ErrorMessage, value, () => RaisePropertiesChanged(nameof(IsSuccess), nameof(IsError))); }
        }

        public bool ShowTypes
        {
            get { return GetProperty(() => ShowTypes); }
            set { SetProperty(() => ShowTypes, value); }
        }

        public bool IsProcessed
        {
            get { return GetProperty(() => IsProcessed); }
            set { SetProperty(() => IsProcessed, value, () => RaisePropertiesChanged(nameof(IsSuccess), nameof(IsError))); }
        }

        public bool IsSuccess => IsProcessed && string.IsNullOrWhiteSpace(ErrorMessage);

        public bool IsError => IsProcessed && !string.IsNullOrWhiteSpace(ErrorMessage);

        public string GetExtension() => FormatExtension(Extension ?? Path.GetExtension(FilePath));

        public async ValueTask<byte[]> GetDataAsync() => Data ?? await FileHelper.ReadBytesAsync(FilePath);

        #region IDataErrorInfo

        string IDataErrorInfo.Error => string.Empty;

        string IDataErrorInfo.this[string columnName] => IDataErrorInfoHelper.GetErrorText(this, columnName);

        #endregion

        public static void BuildMetadata(MetadataBuilder<AddDocumentViewItem> builder)
        {
            builder.Property(x => x.DocumentTypeId)
                .MatchesInstanceRule((x, y) => !(x == null && y.ShowTypes), () => Resources.RequiredErrorMessage);

            builder.Property(x => x.DocumentName)
                .Required(() => Resources.RequiredErrorMessage)
                .MaxLength(2048, () => "Длина поля должна быть короче 2048 символов");
        }

        private string FormatExtension(string extension) => extension.Replace(".", string.Empty).ToLower();
    }
}
