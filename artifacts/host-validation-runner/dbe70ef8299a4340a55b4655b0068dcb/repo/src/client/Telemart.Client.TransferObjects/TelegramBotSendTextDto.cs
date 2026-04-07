using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public sealed class TelegramBotSendTextDto
    {
        public TelegramBotSendTextDto(string chatId, string text, bool disableNotification)
        {
            ChatId = chatId;
            Text = text;
            DisableNotification = disableNotification;
        }

        [JsonProperty("chat_id")]
        public string ChatId { get; set; }

        [JsonProperty("text")]
        public string Text { get; set; }

        [JsonProperty("disable_notification")]
        public bool DisableNotification { get; set; }
    }
}