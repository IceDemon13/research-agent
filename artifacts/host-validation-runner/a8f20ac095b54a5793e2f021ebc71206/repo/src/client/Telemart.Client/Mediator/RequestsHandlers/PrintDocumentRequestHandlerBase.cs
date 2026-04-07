using System;
using System.IO;
using System.IO.Abstractions;
using System.Threading.Tasks;
using MediatR;
using Telemart.Client.Common.Settings.Printing;
using Telemart.Client.Core;
using Telemart.Client.Core.IO;
using Telemart.Client.Mediator.Requests;

namespace Telemart.Client.Mediator.RequestsHandlers
{
    public abstract class PrintDocumentRequestHandlerBase
    {
        private readonly IFileSystem fileSystem;
        private readonly IFileDownloader fileDownloader;
        private readonly IMediator mediator;

        protected PrintDocumentRequestHandlerBase(IFileSystem fileSystem, IFileDownloader fileDownloader, IMediator mediator)
        {
            this.fileSystem = fileSystem;
            this.fileDownloader = fileDownloader;
            this.mediator = mediator;
        }

        protected async Task PrintAsync(string link, string fileName, PrinterSettingsInfo printerSettings, bool showPreview, string base64Pdf, byte[] bytes)
        {
            string printedFolderPath = ApplicationFolders.PrintedFolderPath;

            if (!fileSystem.Directory.Exists(printedFolderPath))
            {
                fileSystem.Directory.CreateDirectory(printedFolderPath);
            }

            string filePath = fileSystem.Path.Combine(printedFolderPath, fileName);

            IFileInfo fileInfo = fileSystem.FileInfo.New(filePath);

            if (!fileInfo.Exists || fileInfo.Length == 0)
            {
                fileSystem.File.Delete(filePath);

                if (string.IsNullOrWhiteSpace(base64Pdf) && bytes == null)
                {
                    await fileDownloader.DownloadFileAsync(link, filePath);
                }
                else
                {
                    byte[] pdfBytes = bytes == null ? Convert.FromBase64String(base64Pdf) : bytes;

                    await File.WriteAllBytesAsync(filePath, pdfBytes);
                }

                fileInfo = fileSystem.FileInfo.New(filePath);

                if (!fileInfo.Exists || fileInfo.Length == 0)
                {
                    fileSystem.File.Delete(filePath);

                    throw new InvalidOperationException("Error downloading file");
                }
            }

            await mediator.Send(new PrintPdfFileRequest(filePath, printerSettings?.Name, printerSettings?.PaperSource, showPreview));
        }
    }
}