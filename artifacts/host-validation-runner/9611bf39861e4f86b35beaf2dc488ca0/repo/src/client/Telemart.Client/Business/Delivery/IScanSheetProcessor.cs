using System.Threading.Tasks;
using Telemart.Client.Data.Requests;
using Telemart.Client.TransferObjects;
using Telemart.Client.ViewModels.Store.CreateScanSheet;
using Telemart.Client.ViewModels.Store.CreateScanSheetForEntities;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.Business.Delivery
{
    internal interface IScanSheetProcessor
    {
        bool Validate(CreateScanSheetModel model);

        Task PrintAsync(CreateScanSheetModel model);

        Task PrintAsync(CreateScanSheetForEntitiesModel model);

        IRestClientGatewayRequest<Result<ScanSheetCreateResponse[]>> CreateRequest(int[] orderIds);

        IRestClientGatewayRequest<Result<ScanSheetCreateResponse[]>> CreateEntityRequest(int entityType, int[] entityIds);
    }
}