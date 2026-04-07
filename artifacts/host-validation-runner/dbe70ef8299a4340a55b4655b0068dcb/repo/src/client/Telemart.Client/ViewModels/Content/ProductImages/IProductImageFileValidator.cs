using System.Collections.Generic;
using System.IO.Abstractions;
using Telemart.Client.ViewModels.Validation;

namespace Telemart.Client.ViewModels.Content.ProductImages
{
    public interface IProductImageFileValidator
    {
        IEnumerable<ValidationResultItem> ValidateFile(IFileInfo file, bool checkFileName = true);
    }
}