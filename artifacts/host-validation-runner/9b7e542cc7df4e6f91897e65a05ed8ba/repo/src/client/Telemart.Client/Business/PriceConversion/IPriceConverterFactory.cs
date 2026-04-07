using System.Collections.Generic;
using System.Threading.Tasks;
using Telemart.Client.TransferObjects;
using Telemart.Common.PriceConversion;

namespace Telemart.Client.Business.PriceConversion
{
    public interface IPriceConverterFactory : IPriceConverterFactoryBase
    {
        Task<IPriceConverter> CreateForSupplierAsync(int supplierId);

        Task<IReadOnlyDictionary<int, IPriceConverter>> CreateForInvoicesAsync(InvoiceDto[] invoices);

        Task<IPriceConverter> CreateForInvoiceAsync(int supplierId, IReadOnlyCollection<ConversionRate> invoiceConversionRates);
    }
}