using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.ServiceRequest
{
    public sealed class CreateServiceRequestDocument : CreateEntityRequestBase<ServiceRequestDocumentSimpleDto, ServiceRequestDocumentDto>
    {
        public CreateServiceRequestDocument(ServiceRequestDocumentDto dto)
            : base(dto, ApiResources.ServiceRequests, dto.ServiceRequestId, "documents")
        {
        }
    }
}