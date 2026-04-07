using System;
using System.Drawing.Printing;
using System.Threading;
using System.Threading.Tasks;
using DevExpress.Mvvm;
using DevExpress.Pdf;
using DevExpress.Xpf.PdfViewer;
using MediatR;
using Telemart.Client.Mediator.Requests;
using Telemart.Client.ViewModels.Dialogs.PdfPreview;

namespace Telemart.Client.Mediator.RequestsHandlers
{
    public sealed class PrintPdfFileRequestHandler : IRequestHandler<PrintPdfFileRequest>, IRequestHandler<PrintPdfRequest>
    {
        private readonly IMessenger _messenger;

        public PrintPdfFileRequestHandler(IMessenger messenger)
        {
            _messenger = messenger;
        }

        public Task Handle(PrintPdfFileRequest message, CancellationToken cancellationToken)
        {
            if (message.ShowPeview || string.IsNullOrWhiteSpace(message.PrinterName))
            {
                _messenger.Send(new PdfPreviewParameter(message.FilePath));
            }
            else
            {
                PdfViewerControl pdfViewerControl = new PdfViewerControl { AsyncDocumentLoad = false };
                pdfViewerControl.OpenDocument(message.FilePath);

                PrinterSettings printerSettings = CreatePrinterSettings(message.PrinterName, message.PaperSource);
                pdfViewerControl.Print(new PdfPrinterSettings(printerSettings), false);

                pdfViewerControl.CloseDocumentCommand.Execute(null);
            }

            return Task.FromResult(Unit.Value);
        }

        public Task Handle(PrintPdfRequest message, CancellationToken cancellationToken)
        {
            if (message.ShowPeview || string.IsNullOrWhiteSpace(message.PrinterName))
            {
                _messenger.Send(new PdfPreviewParameter(message.Body));
            }
            else
            {
                PdfViewerControl pdfViewerControl = new PdfViewerControl
                {
                    AsyncDocumentLoad = false,
                    DocumentSource = message.Body
                };

                PrinterSettings printerSettings = CreatePrinterSettings(message.PrinterName, message.PaperSource);
                pdfViewerControl.Print(new PdfPrinterSettings(printerSettings), false);

                pdfViewerControl.CloseDocumentCommand.Execute(null);
            }

            return Task.FromResult(Unit.Value);
        }

        private static PrinterSettings CreatePrinterSettings(string printerName, string printerPaperSource)
        {
            PrinterSettings printerSettings = new PrinterSettings
            {
                PrinterName = printerName
            };

            if (!string.IsNullOrWhiteSpace(printerPaperSource))
            {
                for (int i = 0; i < printerSettings.PaperSources.Count; i++)
                {
                    PaperSource paperSource = printerSettings.PaperSources[i];

                    if (string.Equals(paperSource.SourceName, printerPaperSource, StringComparison.Ordinal))
                    {
                        printerSettings.DefaultPageSettings.PaperSource = paperSource;
                        break;
                    }
                }
            }

            return printerSettings;
        }
    }
}