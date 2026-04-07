using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telemart.Client.Core.Extensions;
using Telemart.Client.ViewModels.Validation;

namespace Telemart.Client.ViewModels.Content.ProductDescription
{
    public sealed class ProductDescriptionDirectoryProcessor : IProductDescriptionDirectoryProcessor
    {
        private readonly Encoding acceptEncoding;

        private readonly IFileSystem fileSystem;

        private readonly HashSet<string> allowedFileExtentions;

        public ProductDescriptionDirectoryProcessor(IFileSystem fileSystem)
        {
            this.fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));

            allowedFileExtentions = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".txt" };

            acceptEncoding = Encoding.GetEncoding("windows-1251");
        }

        public Task<ProductDescriptionViewItem[]> ProcessAsync(string rootPath)
        {
            if (string.IsNullOrWhiteSpace(rootPath))
            {
                throw new ArgumentException("Root path is empty", nameof(rootPath));
            }

            return fileSystem.Directory.Exists(rootPath)
                ? GetFilesAsync(rootPath)
                : Task.FromResult(Array.Empty<ProductDescriptionViewItem>());
        }

        private Task<ProductDescriptionViewItem[]> GetFilesAsync(string path)
        {
            Task<ProductDescriptionViewItem>[] tasks = fileSystem.DirectoryInfo.New(path)
                .EnumerateFiles()
                .Select(GetItemFromFileAsync)
                .ToArray();

            return Task.WhenAll(tasks);
        }

        private async Task<ProductDescriptionViewItem> GetItemFromFileAsync(IFileInfo fileInfo)
        {
            string text;

            using (Stream stream = fileInfo.OpenRead())
            {
                using (StreamReader reader = new StreamReader(stream, acceptEncoding))
                {
                    text = await reader.ReadToEndAsync();
                }
            }

            return new ProductDescriptionViewItem(
                fileInfo.DirectoryName,
                fileInfo.Name,
                fileInfo.Name.TrimEnd(fileInfo.Extension, StringComparison.OrdinalIgnoreCase),
                text,
                ValidateFile(fileInfo, text).ToArray());
        }

        private IEnumerable<ValidationResultItem> ValidateFile(IFileInfo file, string text)
        {
            if (!allowedFileExtentions.Contains(file.Extension))
            {
                yield return new ValidationResultItem($"Неверное расширение файла: {file.FullName}", false);
            }

            if (!text.Any(x => new[] { 'а', 'п', 'р', 'о' }.Contains(x)))
            {
                yield return new ValidationResultItem($"Файл {file.FullName} должен быть в кодировке windows-1251", false);
            }
        }
    }
}
