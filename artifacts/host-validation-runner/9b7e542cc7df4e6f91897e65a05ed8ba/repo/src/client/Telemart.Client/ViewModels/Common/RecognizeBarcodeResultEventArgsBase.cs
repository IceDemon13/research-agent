namespace Telemart.Client.ViewModels.Common
{
    public abstract class RecognizeBarcodeResultEventArgsBase
    {
        protected RecognizeBarcodeResultEventArgsBase(
            string barcodeText,
            string errorText)
        {
            BarcodeText = barcodeText;
            ErrorText = errorText;
        }

        public string BarcodeText { get; }

        public string ErrorText { get; set; }

        public bool IsValid => string.IsNullOrWhiteSpace(ErrorText);
    }
}
