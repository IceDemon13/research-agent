namespace Telemart.Client.ViewModels.Common.PrintBarcodeParameters
{
    public sealed class QrCodeParameter
    {
        public QrCodeParameter(string url, string title)
        {
            Url = url;
            Title = title;
        }

        public string Url { get; }

        public string Title { get; }
    }
}