using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Telemart.Client.Data.Requests.Features.Order;
using Telemart.Client.Data.Requests.Features.Products;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.ViewModels.Service.ServiceRequests
{
    public sealed class CompensationHelper
    {
        public CompensationHelper(IWebClient webClient)
        {
            WebClient = webClient;
        }

        private IWebClient WebClient { get; }

        public static IEnumerable<ServiceRequestCompensationPrice> GetCompensationPrices(
            ContractorDto contractor,
            bool returnOnBalance,
            IEnumerable<ServiceRequestCompensationPrice> compensationPrices)
        {
            foreach (ServiceRequestCompensationPrice compensationPrice in compensationPrices)
            {
                if (compensationPrice.CurrencyId == Currency.Uah.Id)
                {
                    yield return compensationPrice;
                }
                else if (compensationPrice.CurrencyId == Currency.Usd.Id)
                {
                    if (contractor.CurrencyPermissions?.Any(x => x.CurrencyId == compensationPrice.CurrencyId && x.Sale) == true
                        && contractor.Limit > 1
                        && returnOnBalance)
                    {
                        yield return compensationPrice;
                    }
                }
                else
                {
                    throw new NotSupportedException();
                }
            }
        }

        public async Task<(IReadOnlyCollection<ServiceRequestCompensationPrice> Prices, OrderDto order)> GetProssibleCompensationPricesAsync(int orderId, int productId, bool isPresaleContractor = false)
        {
            OrderDto order = await WebClient.ExecuteApiRequestAsync(new QueryOrder(orderId));

            OrderProductDto[] orderProducts = order.Products.Where(x => x.Product.Id == productId).OrderBy(x => x.PriceOut).ToArray();

            List<ServiceRequestCompensationPrice> prices = new List<ServiceRequestCompensationPrice>();

            if (orderProducts.Any())
            {
                foreach (var orderProduct in orderProducts)
                {
                    ServiceRequestCompensationPrice orderProductPrice = new ServiceRequestCompensationPrice(orderProduct.PriceOut, orderProduct.CurrencyOutId);

                    prices.Add(orderProductPrice);

                    if (isPresaleContractor == false)
                    {
                        ServiceRequestCompensationPrice orderProductOppositeCurrencyPrice = await ConvertPriceAsync(
                            orderProduct.Product.Id,
                            orderProductPrice,
                            orderProductPrice.CurrencyId == Currency.Uah.Id ? Currency.Usd.Id : Currency.Uah.Id);

                        prices.Add(orderProductOppositeCurrencyPrice);
                    }
                }
            }
            else
            {
                prices.Add(new ServiceRequestCompensationPrice(0, Currency.Uah.Id));
                prices.Add(new ServiceRequestCompensationPrice(0, Currency.Usd.Id));
            }

            return (prices, order);
        }

        private async Task<ServiceRequestCompensationPrice> ConvertPriceAsync(int productId, ServiceRequestCompensationPrice fromPrice, int toCurrencyId)
        {
            ServiceRequestCompensationPrice result;

            if (fromPrice.CurrencyId != toCurrencyId)
            {
                ConvertProductPrice request = new ConvertProductPrice(productId, fromPrice.CurrencyId, toCurrencyId, fromPrice.Value);

                ProductPriceConvertResponse response = await WebClient.ExecuteApiRequestAsync(request);

                result = new ServiceRequestCompensationPrice(response.Value, response.CurrencyId);
            }
            else
            {
                result = fromPrice;
            }

            return result;
        }
    }
}