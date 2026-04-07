using Telemart.Client.TransferObjects.ProductCompatibility;

namespace Telemart.Client.Common.Messages
{
    public class AssemblyCompatibilityMessage : EntityMessage<ProductCompatibilityDto>
    {
        public AssemblyCompatibilityMessage(ProductCompatibilityDto entity, MessageType messageType)
            : base(entity, messageType)
        {
        }
    }
}
