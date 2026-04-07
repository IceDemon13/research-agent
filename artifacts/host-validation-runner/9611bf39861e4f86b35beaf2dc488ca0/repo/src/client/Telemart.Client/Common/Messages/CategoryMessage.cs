using Telemart.Client.TransferObjects;

namespace Telemart.Client.Common.Messages
{
    public sealed class CategoryMessage : EntityMessage<CategoryDto>
    {
        public CategoryMessage(CategoryDto entity, MessageType messageType)
            : base(entity, messageType)
        {
        }
    }
}