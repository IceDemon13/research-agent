namespace Telemart.Client.Reports.ReportBuilders.Base
{
    public abstract class ReportPrinterDataBase
    {
        protected ReportPrinterDataBase(bool showPreview)
        {
            ShowPreview = showPreview;
        }

        public bool ShowPreview { get; }
    }
}