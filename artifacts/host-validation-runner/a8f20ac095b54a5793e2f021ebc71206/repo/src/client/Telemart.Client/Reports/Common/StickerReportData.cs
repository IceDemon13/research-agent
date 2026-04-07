using System.Drawing;

namespace Telemart.Client.Reports
{
    public class StickerReportData
    {
        public StickerReportData(int height, int width, Image image)
        {
            Image = image;
            Width = width;
            Height = height;
        }

        public Image Image { get; }

        public int Width { get; }

        public int Height { get; }
    }
}