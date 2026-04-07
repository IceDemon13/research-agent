using Newtonsoft.Json;

namespace Telemart.Client.Common.Settings.Printing
{
    public sealed class PrinterSettingsInfo
    {
        public PrinterSettingsInfo(string name, string paperSource = null)
        {
            Name = name;
            PaperSource = paperSource;
        }

        public PrinterSettingsInfo()
        {
        }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("paper_source")]
        public string PaperSource { get; set; }
    }
}