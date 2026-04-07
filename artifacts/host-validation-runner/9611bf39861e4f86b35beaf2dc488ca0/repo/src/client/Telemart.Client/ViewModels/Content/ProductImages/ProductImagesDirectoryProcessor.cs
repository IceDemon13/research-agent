using System;
using System.IO;
using System.IO.Abstractions;
using System.Linq;

namespace Telemart.Client.ViewModels.Content.ProductImages
{
    public sealed class ProductImagesDirectoryProcessor : IProductImagesDirectoryProcessor
    {
        private readonly IFileSystem fileSystem;
        private readonly IProductImageFileValidator productImageFileValidator;
        private readonly string[] allowedFileExtensions = new[] { ".jpg", ".jpeg", ".png" };

        public ProductImagesDirectoryProcessor(IFileSystem fileSystem, IProductImageFileValidator productImageFileValidator)
        {
            this.fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
            this.productImageFileValidator = productImageFileValidator ?? throw new ArgumentNullException(nameof(productImageFileValidator));
        }

        public ProductImageDirectoryViewItem[] Process(string rootPath)
        {
            if (string.IsNullOrWhiteSpace(rootPath))
            {
                throw new ArgumentException("Root part is empty", nameof(rootPath));
            }

            if (!fileSystem.Directory.Exists(rootPath))
            {
                return Array.Empty<ProductImageDirectoryViewItem>();
            }

            return fileSystem.DirectoryInfo.New(rootPath)
                .EnumerateDirectories()
                .Select(directoryInfo => new ProductImageDirectoryViewItem(
                    rootPath,
                    directoryInfo.Name,
                    directoryInfo.EnumerateFiles()
                        .Where(x => allowedFileExtensions.Contains(Path.GetExtension(x.Name).ToLower()))
                        .Select(fileInfo => new ProductImageFile(fileInfo.Name, fileInfo.Length, productImageFileValidator.ValidateFile(fileInfo).ToArray()))
                        .ToArray()))
                .ToArray();
        }
    }
}