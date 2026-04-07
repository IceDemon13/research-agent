namespace Telemart.Client.ViewModels.Common
{
    public sealed class RecognizeBarcodeMessage
    {
        public RecognizeBarcodeMessage(RecognizeBarcodeMessageType type, string messageText)
        {
            Type = type;
            MessageText = messageText;
        }

        public RecognizeBarcodeMessageType Type { get; set; }

        public string MessageText { get; set; }
    }
}
