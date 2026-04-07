namespace Telemart.Client.Business.Tag
{
    public interface ITagPrinterFactory
    {
        ITagPrinter Create(int tagFormatId, string header, double width, double height);
    }
}