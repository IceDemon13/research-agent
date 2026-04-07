using System.Diagnostics;

namespace Telemart.Client.HtmlToPdf
{
    public static class HtmlToPdfHelper
    {
        public static void PrintHtmlAsPdf(
            string htmlFilePath,
            string pdfPathToSave,
            int height,
            int width,
            int marginTop = 0,
            int marginBottom = 0,
            int marginLeft = 0,
            int marginRight = 0)
        {
            ProcessStartInfo processStartInfo = new ProcessStartInfo(@"Tools\wkhtmltopdf.exe", $"--no-background --print-media-type --page-width {width} --page-height {height} --margin-left {marginLeft} --margin-right {marginRight} --margin-top {marginTop} --margin-bottom {marginBottom} {htmlFilePath} {pdfPathToSave}");

            processStartInfo.CreateNoWindow = true;
            processStartInfo.UseShellExecute = false;

            Process.Start(processStartInfo).WaitForExit();
        }
    }
}