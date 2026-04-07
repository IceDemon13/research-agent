namespace Telemart.Client.ViewModels.Common
{
    public enum RecognizeBarcodeResult
    {
        None = 0,
        Found = 1,
        FoundInSupplier = 2,
        NotFound = 3,
        Error = 4,
        FoundAssembly = 5,
        FoundAdditionalService = 6,
        FoundAdditionalServiceProduct = 7
    }
}