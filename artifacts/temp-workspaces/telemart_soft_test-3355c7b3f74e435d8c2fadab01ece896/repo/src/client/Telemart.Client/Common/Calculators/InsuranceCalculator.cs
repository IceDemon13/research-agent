using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Telemart.Client.Business.PriceConversion;
using Telemart.Client.Data.Requests.Features.Order;
using Telemart.Client.Data.Requests.Features.ServiceRequest;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.Paging;
using Telemart.Client.ViewModels.Service.ServiceMovements;
using Telemart.Client.ViewModels.Service.ServiceRequests;
using Telemart.Client.ViewModels.Store;
using Telemart.Common.PriceConversion;

namespace Telemart.Client.Common.Calculators
{
    public class InsuranceCalculator : IInsuranceCalculator
    {
        private readonly IWebClient _webClient;
        private readonly IPriceConverterFactory _priceConverterFactory;
        private readonly ILogger<InsuranceCalculator> _logger;

        public InsuranceCalculator(IWebClient webClient, IPriceConverterFactory priceConverterFactory, ILogger<InsuranceCalculator> logger)
        {
            _webClient = webClient ?? throw new ArgumentNullException(nameof(webClient));
            _priceConverterFactory = priceConverterFactory;
            _logger = logger;
        }

        public async Task<decimal> CalculateByServiceMovementAsync(ServiceMovementProductViewItem[] products)
        {
            decimal totalPrice = 0;

            IPriceConverter priceConverter = await _priceConverterFactory.CreateAsync();

            try
            {
                foreach (ServiceMovementProductViewItem serviceMovementProductViewItem in products)
                {
                    if (serviceMovementProductViewItem.Price is not null && serviceMovementProductViewItem.CurrencyId is not null)
                    {
                        decimal productPrice = Math.Round(priceConverter.Convert(serviceMovementProductViewItem.Price.Value, serviceMovementProductViewItem.CurrencyId.Value, Currency.UahId, CurrencyTypeIds.UsdMinusId, false));

                        totalPrice += productPrice;
                    }
                }

                ServiceMovementProductViewItem[] productsWithoutPurchasedPrice = products
                    .Where(x => x.Price is null || x.CurrencyId is null)
                    .ToArray();

                if (productsWithoutPurchasedPrice.Length > 0)
                {
                    IEnumerable<ServiceRequestDto> serviceRequests = await GetServiceRequestProductWithoutPurchasedPriceAsync(productsWithoutPurchasedPrice);

                    List<int> productIds = serviceRequests.Select(x => x.ProductId).ToList();

                    List<OrderDto> orders = await GetOrdersProductWithoutPurchasedPriceAsync(serviceRequests);

                    List<int> productWithoutPriceOutIds = new List<int>();

                    foreach (int productId in productIds)
                    {
                        OrderProductDto orderProductDto = orders?.SelectMany(x => x.Products)
                            .FirstOrDefault(y => y.Product != null && y.Product.Id == productId);

                        if (orderProductDto == null || orderProductDto.PriceOut == 0)
                        {
                            productWithoutPriceOutIds.Add(productId);
                        }
                        else
                        {
                            totalPrice += orderProductDto.PriceOut;
                        }
                    }

                    if (productWithoutPriceOutIds.Count > 0)
                    {
                        foreach (int idProduct in productWithoutPriceOutIds)
                        {
                            OrderDto dto = await GetWithoutPriceOutAsync(idProduct);

                            OrderProductDto orderProductDto = dto.Products.FirstOrDefault(x => x.Product != null && x.Product.Id == idProduct);

                            if (orderProductDto is not null)
                            {
                                totalPrice += orderProductDto.PriceOut;
                            }
                        }
                    }
                }
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Failed to execute action \"{CalculateByServiceMovementAsync}\"", nameof(CalculateByServiceMovementAsync));

                return 0;
            }

            return totalPrice;
        }

        private async Task<IReadOnlyCollection<ServiceRequestDto>> GetServiceRequestProductWithoutPurchasedPriceAsync(ServiceMovementProductViewItem[] productsWithoutPurchasedPrice)
        {
            string[] idServiceRequestWithoutPrices = productsWithoutPurchasedPrice.Select(x => x.ServiceRequestId.ToString()).ToArray();

            string allIdServiceRequests = string.Join(",", idServiceRequestWithoutPrices);

            ServiceRequestFilteringItem filteringItem = new ServiceRequestFilteringItem
            {
                RequestNumbers = allIdServiceRequests
            };

            PagedResult<ServiceRequestDto> serviceRequestsInProgress = await _webClient.ExecuteApiRequestAsync(new QueryServiceRequests(filteringItem));

            return serviceRequestsInProgress.Data;
        }

        private async Task<List<OrderDto>> GetOrdersProductWithoutPurchasedPriceAsync(IEnumerable<ServiceRequestDto> serviceRequestDtos)
        {
            string[] ordersIds = serviceRequestDtos.Select(x => x.OrderId.ToString()).ToArray();

            OrderFilteringItem orderFilteringItem = new OrderFilteringItem(string.Empty, new List<int>())
            {
                OrderNumbers = string.Join(", ", ordersIds)
            };

            return await _webClient.ExecuteApiRequestAsync(new QueryOrders(orderFilteringItem)).GetPagedResultDataAsync();
        }

        private async Task<OrderDto> GetWithoutPriceOutAsync(int idProductWithoutPriceOut)
        {
            OrderFilteringItem orderFilteringItem = new OrderFilteringItem(string.Empty, new List<int>())
            {
                ProductIds = new[] { idProductWithoutPriceOut },
                OrderStatuses = new[] { OrderStatus.Done.Id }.ToList(),
                Take = 1
            };

            List<OrderDto> orderDtos = await _webClient.ExecuteApiRequestAsync(new QueryOrders(orderFilteringItem)).GetPagedResultDataAsync();

            return orderDtos.FirstOrDefault();
        }
    }
}