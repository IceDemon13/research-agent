using System;
using System.Drawing.Printing;
using DevExpress.Xpf.Printing;
using DevExpress.XtraPrinting;
using DevExpress.XtraReports;
using DevExpress.XtraReports.UI;

namespace Telemart.Client.Helpers
{
    public class ReportPrintHelper : IReportPrintHelper
    {
        private string PaperSource { get; set; }

        private short Copies { get; set; }

        public void Print(IReport report, string printerName, string paperSource, bool showPreview, short copies = 1)
        {
            PaperSource = paperSource;
            Copies = copies;

            XtraReport xtraReport = report as XtraReport;
            xtraReport.PrinterName = printerName;

            xtraReport.PrintingSystem.StartPrint += OnStartPrint;

            if (showPreview)
            {
                PrintHelper.ShowPrintPreviewDialog(App.Current.MainWindow, report);
            }
            else
            {
                PrinterSettings printerSettings = new PrinterSettings { PrinterName = printerName };

                if (!printerSettings.IsValid)
                {
                    throw new InvalidOperationException($"Printer {printerName} is invalid");
                }

                PrintHelper.PrintDirect(report, printerName);
            }

            xtraReport.PrintingSystem.StartPrint -= OnStartPrint;
        }

        private void OnStartPrint(object sender, PrintDocumentEventArgs e)
        {
            e.PrintDocument.PrinterSettings.Copies = Copies;

            for (int i = 0; i < e.PrintDocument.PrinterSettings.PaperSources.Count; i++)
            {
                if (string.Equals(e.PrintDocument.PrinterSettings.PaperSources[i].SourceName, PaperSource, StringComparison.Ordinal))
                {
                    e.PrintDocument.DefaultPageSettings.PaperSource = e.PrintDocument.PrinterSettings.PaperSources[i];
                    break;
                }
            }
        }
    }
}