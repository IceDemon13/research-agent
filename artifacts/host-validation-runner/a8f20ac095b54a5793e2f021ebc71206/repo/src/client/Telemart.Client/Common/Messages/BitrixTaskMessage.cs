using Telemart.Client.TransferObjects.Bitrix;

namespace Telemart.Client.Common.Messages
{
    public class BitrixTaskMessage : EntityMessage<CategorizedBitrixTaskDto>
    {
        public BitrixTaskMessage(CategorizedBitrixTaskDto entity, MessageType messageType)
            : base(entity, messageType)
        {
        }
    }
}
