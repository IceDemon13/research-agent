using System;
using System.Collections.Generic;
using System.Linq;
using Telemart.Client.Core.Extensions;
using Telemart.Client.ViewModels.Validation;

namespace Telemart.Client.ViewModels.Content.ProductImages
{
    public sealed class ProductImageFile
    {
        public ProductImageFile(string fileName, long fileSize, IReadOnlyCollection<ValidationResultItem> validationResults)
        {
            FileName = fileName ?? throw new ArgumentNullException(nameof(fileName));

            if (fileSize <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(fileSize));
            }

            FileSize = fileSize;

            ValidationResults = validationResults;

            Valid = ValidationResults == null || !ValidationResults.Any();

            if (Valid)
            {
                StringExtensions.TryParseInt32(FileName, out int fileNumber);
                FileNumber = fileNumber;
            }
        }

        public int FileNumber { get; }

        public string FileName { get; }

        public long FileSize { get; }

        public IReadOnlyCollection<ValidationResultItem> ValidationResults { get; }

        public bool Valid { get; }
    }
}