using System.Collections.Generic;

namespace Telemart.Client.ViewModels.Settings.Printing
{
    public sealed class PrinterInfo
    {
        public PrinterInfo(string name, IReadOnlyCollection<string> paperSources)
        {
            Name = name;
            PaperSources = paperSources;
        }

        public string Name { get; }

        public IReadOnlyCollection<string> PaperSources { get; set; }
    }
}
