using System;
using DevExpress.XtraReports;
using MediatR;

namespace Telemart.Client.Mediator.Requests
{
    public sealed class PrintReportRequest : IRequest
    {
        public PrintReportRequest(IReport report, string fileName, bool showPreview, string printerName, string paperSource, short copies = 1)
            : this(report, showPreview, printerName, paperSource, copies)
        {
            FileName = fileName;
        }

        public PrintReportRequest(IReport report, bool showPreview, string printerName, string paperSource, short copies = 1)
            : this(report, showPreview, copies)
        {
            PrinterName = printerName;
            PaperSource = paperSource;
        }

        public PrintReportRequest(IReport report, string fileName, bool showPreview, short copies = 1)
        {
            FileName = fileName;
            Report = report ?? throw new ArgumentNullException(nameof(report));
            ShowPreview = showPreview;
            Copies = copies;
        }

        public PrintReportRequest(IReport report, bool showPreview, short copies = 1)
        {
            Report = report ?? throw new ArgumentNullException(nameof(report));
            ShowPreview = showPreview;
            Copies = copies;
        }

        public string FileName { get; }

        public short Copies { get; }

        public string PaperSource { get; }

        public string PrinterName { get; }

        public IReport Report { get; }

        public bool ShowPreview { get; }
    }
}