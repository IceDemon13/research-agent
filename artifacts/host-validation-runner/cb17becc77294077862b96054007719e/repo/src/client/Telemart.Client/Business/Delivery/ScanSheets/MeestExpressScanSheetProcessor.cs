using System.Linq;
using System.Threading.Tasks;
using MediatR;
using Telemart.Client.Data.Requests;
using Telemart.Client.Data.Requests.Features.Carry;
using Telemart.Client.Dictionaries;
using Telemart.Client.Mediator.Requests;
using Telemart.Client.TransferObjects;
using Telemart.Client.ViewModels.Store.CreateScanSheet;
using Telemart.Client.ViewModels.Store.CreateScanSheetForEntities;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.Business.Delivery.ScanSheets
{
    internal sealed class MeestExpressScanSheetProcessor : IScanSheetProcessor
    {
        public MeestExpressScanSheetProcessor(IMediator mediator)
        {
            Mediator = mediator;
        }

        private IMediator Mediator { get; }

        public bool Validate(CreateScanSheetModel model)
        {
            return true;
        }

        public async Task PrintAsync(CreateScanSheetModel model)
        {
            foreach (Task<Unit> task in model.Result.Select(x => Mediator.Send(new PrintScanSheetRequest(x.Ref, x.Link, true))))
            {
                await task;
            }
        }

        public IRestClientGatewayRequest<Result<ScanSheetCreateResponse[]>> CreateRequest(int[] orderIds)
        {
            return new CreateScanSheetRequest(orderIds, ScanSheetPrefix.me);
        }

        public Task PrintAsync(CreateScanSheetForEntitiesModel model)
        {
            throw new System.NotImplementedException();
        }

        public IRestClientGatewayRequest<Result<ScanSheetCreateResponse[]>> CreateEntityRequest(int entityType, int[] entityIds)
        {
            throw new System.NotImplementedException();
        }
    }
}