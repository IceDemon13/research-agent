using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Telegram
{
    public sealed class SupportBotSendText : CallActionWithBodyRequestBase<object, TelegramBotSendTextDto>
    {
        public SupportBotSendText(TelegramBotSendTextDto dto)
            : base(dto, ApiResources.Support, "send/text")
        {
        }
    }
}