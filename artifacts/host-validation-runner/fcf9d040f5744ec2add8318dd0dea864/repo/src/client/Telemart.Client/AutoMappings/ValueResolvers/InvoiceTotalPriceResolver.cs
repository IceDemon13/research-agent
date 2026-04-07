using System.Linq;
using AutoMapper;
using Telemart.Client.Business;
using Telemart.Client.TransferObjects;
using Telemart.Client.ViewModels.Store;

namespace Telemart.Client.AutoMappings.ValueResolvers
{
    public sealed class InvoiceTotalPriceResolver : IValueResolver<InvoiceDto, InvoiceViewItem, Prices>
    {
        public Prices Resolve(InvoiceDto source, InvoiceViewItem destination, Prices destMember, ResolutionContext context)
        {
            Prices prices = new Prices(0, 0, 0);

            if (source?.InvoiceProducts != null)
            {
                foreach (Price price in source.InvoiceProducts.Select(x => new Price(x.Price * x.Quantity, x.CurrencyId)))
                {
                    prices += price;
                }
            }

            return prices;
        }
    }
}