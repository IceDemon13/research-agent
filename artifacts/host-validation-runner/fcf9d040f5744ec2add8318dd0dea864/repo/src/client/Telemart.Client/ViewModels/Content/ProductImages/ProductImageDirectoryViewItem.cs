using System.Collections.Generic;
using System.Linq;
using DevExpress.Mvvm;
using DevExpress.Mvvm.DataAnnotations;
using Telemart.Client.Properties;
using Telemart.Client.ViewModels.Validation;

namespace Telemart.Client.ViewModels.Content.ProductImages
{
    public sealed class ProductImageDirectoryViewItem : BindableBase
    {
        public ProductImageDirectoryViewItem(string rootPath, string directoryName, ProductImageFile[] files)
        {
            Path = rootPath;
            DirectoryName = directoryName;
            Files = files;

            ProductId = null;
            ProductName = null;
        }

        public string Path
        {
            get { return GetProperty(() => Path); }
            private set { SetProperty(() => Path, value); }
        }

        public string DirectoryName
        {
            get { return GetProperty(() => DirectoryName); }
            private set { SetProperty(() => DirectoryName, value); }
        }

        public int? ProductId
        {
            get { return GetProperty(() => ProductId); }
            set { SetProperty(() => ProductId, value); }
        }

        public string ProductName
        {
            get { return GetProperty(() => ProductName); }
            set { SetProperty(() => ProductName, value); }
        }

        public bool IsProcessed
        {
            get { return GetProperty(() => IsProcessed); }
            set { SetProperty(() => IsProcessed, value); }
        }

        public ProductImageFile[] Files { get; }

        public bool Valid => Files.Any(x => x.Valid);

        public static void BuildMetadata(MetadataBuilder<ProductImageDirectoryViewItem> builder)
        {
            builder.Property(x => x.ProductName).Required(() => Resources.RequiredErrorMessage);
        }

        public IEnumerable<ValidationResultItem> GetValidationResults()
        {
            if (Files.Length == 0)
            {
                yield return new ValidationResultItem($"Папка не содержит файлов: {DirectoryName}", false);
            }

            foreach (ValidationResultItem result in Files.OrderBy(x => x.FileNumber).SelectMany(x => x.ValidationResults))
            {
                yield return result;
            }
        }
    }
}
