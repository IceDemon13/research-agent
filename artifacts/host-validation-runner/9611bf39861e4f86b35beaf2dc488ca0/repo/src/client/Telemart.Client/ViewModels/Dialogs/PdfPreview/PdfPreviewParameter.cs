namespace Telemart.Client.ViewModels.Dialogs.PdfPreview
{
    public class PdfPreviewParameter
    {
        public PdfPreviewParameter(object source)
        {
            Source = source;
        }

        public object Source { get; }
    }
}