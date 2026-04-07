using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.ServiceRequest.Actions
{
    public sealed class CreateServiceRequestTrackNumber
        : CallEntityActionWithBodyRequestResultBase<ServiceRequestCreateTrackNumberResultDto, ServiceRequestCreateTrackNumberDto>
    {
        public CreateServiceRequestTrackNumber(
            int id,
            string recipient,
            string phone,
            int carryId,
            DeliveryDataDto deliveryData,
            int places,
            double weight,
            decimal insurance,
            bool addToApplication,
            int? packageWidth,
            int? packageLength,
            int? packageHeight)
            : base(
                id,
                new ServiceRequestCreateTrackNumberDto(id, recipient, phone, carryId, deliveryData, places, weight, insurance, addToApplication, packageWidth, packageLength, packageHeight),
                ApiResources.ServiceRequests,
                "create_tn")
        {
        }
    }
}