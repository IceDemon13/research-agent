using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public sealed class TelegramBotSendDocumentDto
    {
        public TelegramBotSendDocumentDto(
            byte[] document,
            string documentName,
            string caption,
            bool disableNotification,
            string chatId = null)
        {
            Document = document;
            DocumentName = documentName;
            Caption = caption;
            DisableNotification = disableNotification;
            ChatId = chatId;
        }

        [JsonProperty("chat_id")]
        public string ChatId { get; set; }

        [JsonProperty("document")]
        public byte[] Document { get; set; }

        [JsonProperty("document_name")]
        public string DocumentName { get; set; }

        [JsonProperty("caption")]
        public string Caption { get; set; }

        [JsonProperty("disable_notification")]
        public bool DisableNotification { get; set; }
    }
}