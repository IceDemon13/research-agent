using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO.Abstractions;
using Humanizer;
using Telemart.Client.Data.Options;
using Telemart.Client.ViewModels.Validation;

namespace Telemart.Client.ViewModels.Content.ProductImages
{
    public sealed class ProductImageFileValidator : IProductImageFileValidator
    {
        private readonly FileUploadOptions _options;

        public ProductImageFileValidator(FileUploadOptions options)
        {
            _options = options;
        }

        public IEnumerable<ValidationResultItem> ValidateFile(IFileInfo file, bool checkFileName)
        {
            if (checkFileName)
            {
                string fileNameWithoutExtension = Core.Extensions.StringExtensions.TrimEnd(
                    file.Name,
                    file.Extension,
                    StringComparison.OrdinalIgnoreCase);

                if (!int.TryParse(fileNameWithoutExtension, NumberStyles.Integer, CultureInfo.InvariantCulture, out int _))
                {
                    yield return new ValidationResultItem($"Неверное название файла: {file.FullName}", true);
                }
            }

            if (file.Length.Bytes().Megabytes > _options.PhotoMaxSizeMb)
            {
                yield return new ValidationResultItem($"Файл превышает {_options.PhotoMaxSizeMb.ToString(CultureInfo.InvariantCulture)} Мб: {file.FullName}", true);
            }
        }
    }
}