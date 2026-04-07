using System.Drawing;

namespace Telemart.Client.Reports
{
    public class TextReportData
    {
        public TextReportData(string text, int width, Image image = null)
        {
            Text = text;
            Width = width;
            Image = image;
            ImageVisible = Image != null;
        }

        public string Text { get; }

        public Image Image { get; }

        public bool ImageVisible { get; }

        public int Width { get; }
    }
}