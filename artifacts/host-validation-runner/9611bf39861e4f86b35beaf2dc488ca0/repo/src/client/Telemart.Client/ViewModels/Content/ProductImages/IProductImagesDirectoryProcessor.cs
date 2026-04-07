namespace Telemart.Client.ViewModels.Content.ProductImages
{
    public interface IProductImagesDirectoryProcessor
    {
        ProductImageDirectoryViewItem[] Process(string rootPath);
    }
}