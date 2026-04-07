using System;
using System.Collections.Generic;
using System.Linq;
using DevExpress.Mvvm;
using Humanizer;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.ViewModels.Content.FeatureImages
{
    public sealed class FeatureIconValidator : IFeatureIconValidator
    {
        private const int MaxSizeImage = 100;

        public Result ProcessValidateFile(IFileInfo file)
        {
            if (file == null)
            {
                throw new ArgumentException("No information about images file", nameof(file));
            }

            IEnumerable<string> validates = ValidateFile(file);

            if (validates?.Any() == true)
            {
                return Result.Error("Failed validate icon", validates.ToList());
            }

            return Result.Success();
        }

        private IEnumerable<string> ValidateFile(IFileInfo file)
        {
            if (!file.Exists)
            {
                yield return "Файла не существует";
            }

            if (file.Length > MaxSizeImage.Kilobytes().Bytes)
            {
                yield return "Иконка превышает 100 kB";
            }
        }
    }
}