using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Telegram
{
    public sealed class SupportBotSendDocument : CallActionWithBodyRequestResultBase<object, TelegramBotSendDocumentDto>
    {
        public SupportBotSendDocument(TelegramBotSendDocumentDto dto)
            : base(dto, ApiResources.Support, "send/document")
        {
        }
    }
}