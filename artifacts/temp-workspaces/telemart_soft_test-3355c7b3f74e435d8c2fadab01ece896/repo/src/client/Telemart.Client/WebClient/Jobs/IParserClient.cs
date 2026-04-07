using System.Threading.Tasks;
using Telemart.Client.TransferObjects.ParserRequests;

namespace Telemart.Client.WebClient.Jobs
{
    public interface IParserClient
    {
        Task<ResultMessage> SaveProductsAsync(SaveProductsRequest message);

        Task<ResultMessage> ClearAvailableBySupplierWarehouseIdAsync(ClearAvailableBySupplierWarehouseIdRequest request);
    }
}