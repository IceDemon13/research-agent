using System.Threading.Tasks;

namespace Telemart.Client.ViewModels.Content.ProductDescription
{
    public interface IProductDescriptionDirectoryProcessor
    {
        Task<ProductDescriptionViewItem[]> ProcessAsync(string rootPath);
    }
}
