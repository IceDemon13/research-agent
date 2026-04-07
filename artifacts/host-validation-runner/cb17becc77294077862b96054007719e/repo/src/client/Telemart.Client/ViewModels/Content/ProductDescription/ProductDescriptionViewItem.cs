using System.Collections.Generic;
using System.Linq;
using DevExpress.Mvvm;
using DevExpress.Mvvm.DataAnnotations;
using Telemart.Client.Properties;
using Telemart.Client.ViewModels.Validation;

namespace Telemart.Client.ViewModels.Content.ProductDescription
{
    public sealed class ProductDescriptionViewItem : BindableBase
    {
        public ProductDescriptionViewItem(
            string filePath,
            string fileName,
            string fileNameWithoutExtension,
            string fileText,
            IReadOnlyCollection<ValidationResultItem> validationResults)
        {
            FilePath = filePath;
            FileName = fileName;
            FileNameWithoutExt = fileNameWithoutExtension;
            Description = fileText;
            ValidationResults = validationResults;
            IsProcessed = false;
        }

        public string FilePath
        {
            get { return GetProperty(() => FilePath); }
            private set { SetProperty(() => FilePath, value); }
        }

        public string FileName
        {
            get { return GetProperty(() => FileName); }
            private set { SetProperty(() => FileName, value); }
        }

        public string FileNameWithoutExt
        {
            get { return GetProperty(() => FileNameWithoutExt); }
            private set { SetProperty(() => FileNameWithoutExt, value); }
        }

        public string Description
        {
            get { return GetProperty(() => Description); }
            private set { SetProperty(() => Description, value); }
        }

        public string ProductName
        {
            get { return GetProperty(() => ProductName); }
            set { SetProperty(() => ProductName, value); }
        }

        public int? ProductId
        {
            get { return GetProperty(() => ProductId); }
            set { SetProperty(() => ProductId, value); }
        }

        public int LanguageId
        {
            get { return GetProperty(() => LanguageId); }
            set { SetProperty(() => LanguageId, value); }
        }

        public string ErrorMessage
        {
            get { return GetProperty(() => ErrorMessage); }
            set { SetProperty(() => ErrorMessage, value, () => RaisePropertiesChanged(nameof(IsSuccess), nameof(IsError))); }
        }

        public bool IsProcessed
        {
            get { return GetProperty(() => IsProcessed); }
            set { SetProperty(() => IsProcessed, value, () => RaisePropertiesChanged(nameof(IsSuccess), nameof(IsError))); }
        }

        public bool IsSuccess => IsProcessed && string.IsNullOrWhiteSpace(ErrorMessage);

        public bool IsError => IsProcessed && !string.IsNullOrWhiteSpace(ErrorMessage);

        public bool Valid => ValidationResults == null || !ValidationResults.Any();

        public IReadOnlyCollection<ValidationResultItem> ValidationResults { get; }

        public static void BuildMetadata(MetadataBuilder<ProductDescriptionViewItem> builder)
        {
            builder.Property(x => x.ProductName).Required(() => Resources.RequiredErrorMessage);
        }
    }
}
