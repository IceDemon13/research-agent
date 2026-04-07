using DevExpress.Mvvm;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.ViewModels.Content.FeatureImages
{
    public interface IFeatureIconValidator
    {
        Result ProcessValidateFile(IFileInfo file);
    }
}