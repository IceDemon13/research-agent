using System.IO.Abstractions;
using System.Threading;
using System.Threading.Tasks;
using DevExpress.XtraReports.UI;
using MediatR;
using Telemart.Client.Core;
using Telemart.Client.Helpers;
using Telemart.Client.Mediator.Requests;

namespace Telemart.Client.Mediator.RequestsHandlers
{
    public sealed class PrintReportRequestHandler : IRequestHandler<PrintReportRequest>
    {
        public PrintReportRequestHandler(IReportPrintHelper reportPrintHelper, IFileSystem fileSystem)
        {
            ReportPrintHelper = reportPrintHelper;
            FileSystem = fileSystem;
        }

        private IReportPrintHelper ReportPrintHelper { get; }

        private IFileSystem FileSystem { get; }

        public Task Handle(PrintReportRequest message, CancellationToken cancellationToken)
        {
            ReportPrintHelper.Print(message.Report, message.PrinterName, message.PaperSource, message.ShowPreview, message.Copies);

            if (!string.IsNullOrWhiteSpace(message.FileName))
            {
                string printedFolderPath = ApplicationFolders.PrintedFolderPath;

                if (!FileSystem.Directory.Exists(printedFolderPath))
                {
                    FileSystem.Directory.CreateDirectory(printedFolderPath);
                }

                string filePath = FileSystem.Path.Combine(printedFolderPath, message.FileName);

                XtraReport report = (XtraReport)message.Report;

                report.ExportToPdf(filePath);
            }

            return Task.CompletedTask;
        }
    }
}