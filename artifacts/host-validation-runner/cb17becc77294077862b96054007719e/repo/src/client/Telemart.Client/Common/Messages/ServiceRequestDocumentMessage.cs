using Telemart.Client.TransferObjects;

namespace Telemart.Client.Common.Messages
{
    public class ServiceRequestDocumentMessage : EntityMessage<ServiceRequestDocumentSimpleDto>
    {
        public ServiceRequestDocumentMessage(ServiceRequestDocumentSimpleDto entity, MessageType messageType)
            : base(entity, messageType)
        {
        }
    }
}
