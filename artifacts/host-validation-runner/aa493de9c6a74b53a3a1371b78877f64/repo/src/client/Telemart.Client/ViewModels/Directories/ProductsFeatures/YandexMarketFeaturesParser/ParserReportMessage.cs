using System.Windows.Media;

namespace Telemart.Client.ViewModels.Directories.ProductsFeatures.YandexMarketFeaturesParser
{
    public class ParserReportMessage
    {
        public const int BlackColor = 0;
        public const int RedColor = 1;
        public const int GreenColor = 2;

        public ParserReportMessage(string message, int colorConstant = 0)
        {
            Message = message;

            switch (colorConstant)
            {
                case RedColor:
                    Brush = Brushes.Red;
                    break;
                case GreenColor:
                    Brush = Brushes.Green;
                    break;
                default:
                    Brush = Brushes.Black;
                    break;
            }
        }

        public string Message { get; }

        public Brush Brush { get; }
    }
}
