using System;
using Telemart.Client.Dictionaries;

namespace Telemart.Client.Business.Tag
{
    public sealed class TagPrinterFactory : ITagPrinterFactory
    {
        public ITagPrinter Create(int tagFormatId, string header, double width, double height)
        {
            ITagPrinter tagPrinter;

            if (tagFormatId == TagFormat.Small.Id)
            {
                tagPrinter = new SmallTagPrinter(header, width, height);
            }
            else if (tagFormatId == TagFormat.Normal.Id)
            {
                tagPrinter = new DefaultTagPrinter(header, width, height);
            }
            else if (tagFormatId == TagFormat.Promo.Id)
            {
                tagPrinter = new DefaultTagPrinter(header, width, height);
            }
            else
            {
                throw new NotSupportedException($"Not supported tag format id:{tagFormatId}");
            }

            return tagPrinter;
        }
    }
}