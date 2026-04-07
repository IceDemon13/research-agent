using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Telegram
{
    public sealed class ContractorBotSendDocument : CallActionWithBodyRequestResultBase<object, TelegramBotSendDocumentDto>
    {
        public ContractorBotSendDocument(TelegramBotSendDocumentDto dto)
            : base(dto, ApiResources.Contractor, "send/document")
        {
        }
    }
}