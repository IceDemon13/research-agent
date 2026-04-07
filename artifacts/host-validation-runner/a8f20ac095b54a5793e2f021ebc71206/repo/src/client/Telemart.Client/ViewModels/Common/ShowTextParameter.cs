using Telemart.Client.Common.Settings.Printing;

namespace Telemart.Client.ViewModels.Common
{
    public class ShowTextParameter
    {
        public ShowTextParameter(string caption, string body, bool monospace, int reportWidth = 0, PrinterSettingsInfo printer = null)
        {
            Caption = caption;
            Body = body;
            Monospace = monospace;
            Printer = printer;
            ReportWidth = reportWidth;
        }

        public string Caption { get; }

        public string Body { get; }

        public bool Monospace { get; }

        public int ReportWidth { get; }

        public PrinterSettingsInfo Printer { get; }
    }
}