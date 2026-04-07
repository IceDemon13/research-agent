using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DevExpress.Mvvm;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Telemart.Client.Common;
using Telemart.Client.Common.Messages;
using Telemart.Client.Common.Services;
using Telemart.Client.Core.Extensions;
using Telemart.Client.Data.Requests.Features.AssemblyService.Actions;
using Telemart.Client.Data.Requests.Features.Catalog;
using Telemart.Client.Data.Requests.Features.Contractor;
using Telemart.Client.Data.Requests.Features.Promo;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.Customer;
using Telemart.Client.TransferObjects.Paging;
using Telemart.Client.TransferObjects.Promo;
using Telemart.Client.ViewModels.Common;
using Telemart.Client.ViewModels.Store.Order;
using Telemart.Common.TreeStructure;

namespace Telemart.Client.Business.Order
{
    public sealed class OrderViewProvider
    {
        public const string OrderViewName = "OrderView";

        public OrderViewProvider(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessenger messenger,
            IMessageFacadeService messageFacadeService,
            IServiceProvider serviceProvider,
            ILogger<OrderViewProvider> logger)
        {
            WebClient = webClient;
            Dictionaries = dictionaries;
            Messenger = messenger;
            MessageFacadeService = messageFacadeService;
            ServiceProvider = serviceProvider;
            Logger = logger;
        }

        private IDictionaries Dictionaries { get; }

        private ILogger<OrderViewProvider> Logger { get; }

        private IMessageFacadeService MessageFacadeService { get; }

        private IMessenger Messenger { get; }

        private IWebClient WebClient { get; }

        private IServiceProvider ServiceProvider { get; }

        public async Task<OrderViewModel> AddOrderAsync(OrderCreateViewMessage message)
        {
            OrderViewModel orderViewModel;

            try
            {
                OrderDto order = new OrderDto
                {
                    Id = 0,
                    CreatedOn = DateTime.Now,
                    CreatedBy = WebClient.AuthenticatedEmployee.Id,
                    PackageDeliveryPaid = 1,
                    PackageDeliveryCost = 0,
                    DeliveryData = new DeliveryDataDto(),
                    Products = new List<OrderProductDto>(),
                    PromoCodes = new List<OrderPromoCodeDto>(),
                    OrderPayments = new List<OrderPaymentDto>()
                };

                ContractorTemplateDto template = message.ContractorTemplate;
                CustomerDto customer = message.Customer;

                if (template != null)
                {
                    order.ClientId = template.ClientId;
                    order.CityId = template.CityId;
                    order.CarryId = template.CarryId;
                    order.WarehouseId = template.WarehouseId;
                    order.Address = template.Address;
                    order.DeliveryData = template.DeliveryData;
                    order.LastName = template.LastName;
                    order.FirstName = template.FirstName;
                    order.MiddleName = template.MiddleName;
                    order.Phone = template.Phone;
                    order.Phone2 = template.Phone2;
                    order.Email = template.Email;
                }
                else if (customer != null)
                {
                    order.ClientId = customer.ContractorId;
                    order.LastName = customer.LastName;
                    order.FirstName = customer.FirstName;
                    order.MiddleName = customer.MiddleName;
                    order.Phone = customer.Phone1;
                    order.Phone2 = customer.Phone2;
                    order.Email = customer.Email;
                }

                orderViewModel = CreateOrderViewModel();

                await orderViewModel.InitializeAddAsync(order).ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Failed to add order");
                Messenger.Send(new OnOrderCreationFinishedMessage());
                throw;
            }

            return orderViewModel;
        }

        public async Task<OrderViewModel> AddOrderAsync(OrderCopyViewMessage message)
        {
            OrderViewModel orderViewModel;

            try
            {
                OrderDto order = new OrderDto
                {
                    Id = 0,
                    StateId = OrderStatus.Received.Id,
                    CreatedOn = DateTime.Now,
                    CreatedBy = WebClient.AuthenticatedEmployee.Id,
                    PackageDeliveryPaid = 1,
                    ClientId = message.CopiedOrder.ClientId,
                    SubdivisionId = message.CopiedOrder.SubdivisionId,
                    Fio = message.CopiedOrder.Fio,
                    FirstName = message.CopiedOrder.FirstName,
                    MiddleName = message.CopiedOrder.MiddleName,
                    LastName = message.CopiedOrder.LastName,
                    Phone = message.CopiedOrder.Phone.GetLocalPhoneNumber(),
                    Phone2 = message.CopiedOrder.Phone2?.GetLocalPhoneNumber(),
                    Email = message.CopiedOrder.Email,
                    CityId = message.CopiedOrder.CityId,
                    CarryId = message.CopiedOrder.CarryId,
                    WarehouseId = message.CopiedOrder.WarehouseId
                };

                Payment payment = Dictionaries.GetItemById<Payment>(message.CopiedOrder.PaymentId);

                order.PaymentId = payment.OnlyCreateOnWeb ? Payment.CashId : payment.Id;
                order.Address = message.CopiedOrder.Address;
                order.DeliveryData = new DeliveryDataDto
                {
                    CityId = message.CopiedOrder.DeliveryData.CityId,
                    PlaceId = message.CopiedOrder.DeliveryData.PlaceId,
                    Street = message.CopiedOrder.DeliveryData.Street,
                    House = message.CopiedOrder.DeliveryData.House,
                    Flat = message.CopiedOrder.DeliveryData.Flat,
                    Extra = message.CopiedOrder.DeliveryData.Extra,
                    MaxAllowedWeight = message.CopiedOrder.DeliveryData.MaxAllowedWeight,
                    Address = message.CopiedOrder.DeliveryData.Address,
                    AddressUkr = message.CopiedOrder.DeliveryData.AddressUkr,
                    AddressEn = message.CopiedOrder.DeliveryData.AddressEn,
                    Index = message.CopiedOrder.DeliveryData.Index
                };

                Dictionary<int, int?> newOrderFolderIds = message.CopiedOrder.Folders.ToDictionary(x => x.Id, x => x.TypeId == OrderFolderType.BundleId ? (int?)null : new Random().GetRandomId());

                order.Folders = message.CopiedOrder.Folders
                    .Where(x => x.TypeId != OrderFolderType.AssembledComputerRuleId && x.TypeId != OrderFolderType.BundleId)
                    .Select(x => new OrderFolderDto()
                    {
                        Id = newOrderFolderIds[x.Id].Value,
                        ProductId = x.ProductId,
                        Price = x.Price,
                        TypeId = x.TypeId,
                        Quantity = x.Quantity,
                        Name = x.Name,
                        FreeDelivery = x.FreeDelivery
                    }).ToList();

                int[] orderFoldersToDelete = message.CopiedOrder.Folders
                    .Where(x => x.TypeId == OrderFolderType.AssembledComputerRuleId)
                    .Select(x => x.Id)
                    .ToArray();

                order.Products = await GetCopiedOrderProductsAsync(message.CopiedOrder, order.SubdivisionId, newOrderFolderIds, orderFoldersToDelete);

                order.PromoCodes = new List<OrderPromoCodeDto>();
                order.OrderPayments = new List<OrderPaymentDto>();

                foreach (OrderFolderDto orderFolder in order.Folders.Where(x => x.TypeId != OrderFolderType.AssembledComputerRuleId))
                {
                    var folderProducts = order.Products
                        .Where(x => x.AssemblyIncluded && x.OrderFolderId == orderFolder.Id).ToArray();

                    var assemblyServiceProduct = folderProducts.FirstOrDefault(x => x.Product.Id == Constants.AssemblyServiceProductId);

                    if (assemblyServiceProduct != null)
                    {
                        var assemblyPrice = folderProducts
                            .Where(x => x.Product.Id != Constants.AssemblyServiceProductId)
                            .Sum(x => (x.AssemblyQuantity ?? 1) * x.PriceOut);

                        var calculatePriceResult = await WebClient.ExecuteApiRequestAsync(new CalculateAssemblyServiceProductPrice(assemblyPrice));

                        if (calculatePriceResult.IsSuccess)
                        {
                            assemblyServiceProduct.Price = calculatePriceResult.Data.Price;
                            assemblyServiceProduct.PriceOut = calculatePriceResult.Data.Price;
                        }
                    }
                }

                orderViewModel = CreateOrderViewModel();

                await orderViewModel.InitializeAddAsync(order).ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Failed to copy order. OrderId:{OrderId}", message?.CopiedOrder?.Id);
                MessageFacadeService.ShowNotificationError("Произошла ошибка при копировании заказа");
                throw;
            }

            return orderViewModel;
        }

        public async Task<OrderViewModel> EditOrderAsync(int orderId, bool editMode)
        {
            OrderViewModel orderViewModel = CreateOrderViewModel();

            await orderViewModel.InitializeEditAsync(orderId, editMode);

            return orderViewModel;
        }

        private async Task<List<OrderProductDto>> GetCopiedOrderProductsAsync(
            OrderDto copiedOrder,
            int subdivisionId,
            IReadOnlyDictionary<int, int?> newOrderFolderIds,
            int[] orderFolderToDeleteIds)
        {
            if (copiedOrder.Products.Count == 0)
            {
                return new List<OrderProductDto>();
            }

            ContractorDto contractor = await WebClient.ExecuteApiRequestAsync(new QueryContractor(copiedOrder.ClientId));

            QueryProductByIdsDto queryProductByIdsDto = new QueryProductByIdsDto(
                copiedOrder.Products
                    .Where(x => x.ParentRecordId is null && !orderFolderToDeleteIds.Contains(x.OrderFolderId ?? 0))
                    .Select(x => x.Product.Id)
                    .ToArray(),
                copiedOrder.ClientId,
                true,
                true,
                includePrices: true);

            List<ProductDto> refreshedProductsWithGifts = await WebClient.ExecuteCatalogApiRequestAsync(new QueryProductByIds(queryProductByIdsDto));

            int[] productIdsWithGifts = refreshedProductsWithGifts.Select(x => x.Id).Distinct().Union(refreshedProductsWithGifts.SelectMany(x => x.Gifts.Select(y => y.Id))).ToArray();

            PagedResult<PromoDto> promos = await WebClient.ExecuteApiRequestAsync(new QueryPromos(new PromoFilteringItem(subdivisionId, productIdsWithGifts, true)));

            if (copiedOrder.Products.Any(x => orderFolderToDeleteIds.Contains(x.OrderFolderId ?? 0)))
            {
                MessageFacadeService.ShowNotificationWarning("При копировании заказа, были удалены не завершенные конфигурации");
            }

            return GetCopiedOrderProducts().ToList();

            IEnumerable<OrderProductDto> GetCopiedOrderProducts()
            {
                Random random = new Random();

                bool chargeMaxBonusQuantity = copiedOrder.CreatedOn.AddMonths(1) > DateTime.Now && (copiedOrder.StateId == OrderStatus.Canceled.Id || copiedOrder.StateId == OrderStatus.DidNotOrder.Id || copiedOrder.StateId == OrderStatus.DidNotTake.Id);

                foreach (ProductDto product in refreshedProductsWithGifts)
                {
                    foreach (OrderProductDto orderProduct in copiedOrder.Products.Where(x => x.Product.Id == product.Id))
                    {
                        OrderProductSource source = product.TypeId == ProductType.GuestProductId
                                ? new GuestOrderProductSource(copiedOrder.AdditionalServiceWarehouseId ?? copiedOrder.WarehouseId ?? 0, "Клиент", DateTime.Now.AddDays(3))
                                : null;

                        int? bonusesToCharge = null;

                        if (product.BonusesToCharge.HasValue || orderProduct.BonusesToCharge.HasValue)
                        {
                            bonusesToCharge = chargeMaxBonusQuantity ? Math.Max(product.BonusesToCharge ?? 0, orderProduct.BonusesToCharge ?? 0) : product.BonusesToCharge;
                        }

                        OrderProductDto copiedOrderProduct = new OrderProductDto
                        {
                            Id = random.GetRandomId(),
                            ParentRecordId = null,
                            Product = new ProductSimpleDto
                            {
                                Id = product.Id,
                                Name = product.Name,
                                Weight = product.Weight ?? 0,
                                NameFullRu = product.NameFullRu,
                                NameFullUkr = product.NameFullUa,
                                TypeId = product.TypeId,
                                Prices = product.Prices,
                                BonusesToCharge = bonusesToCharge
                            },
                            BonusesToCharge = bonusesToCharge,
                            SourceId = source?.Id ?? 0,
                            WarehouseId = source?.WarehouseId,
                            SourceDate = source?.SourceDate,
                            SourceText = source?.SourceText,
                            Quantity = orderProduct.Quantity,
                            StateId = OrderProductStatus.New.Id,
                            PriceOut = product.Price,
                            CurrencyOutId = product.CurrencyId,
                            Price = product.Price,
                            PriceId = contractor.PriceTypeId,
                            CurrencyId = product.CurrencyId,
                            AssemblyId = orderProduct.AssemblyId,
                            OrderFolderId = orderProduct.OrderFolderId.HasValue ? newOrderFolderIds[orderProduct.OrderFolderId.Value] : null,
                            AssemblyQuantity = orderProduct.AssemblyQuantity,
                            AssemblyIncluded = orderProduct.AssemblyIncluded,
                            IsGift = false,
                            IsAdditionalService = false,
                            Promo = promos.Data.FirstOrDefault(x => x.ProductIds.Contains(product.Id))
                        };

                        yield return copiedOrderProduct;

                        if (product.Gifts == null)
                        {
                            continue;
                        }

                        ProductAdditionalServiceDto[] availAdditionalServices = TreeStructureHelper
                            .Deconstruct(product.AdditionalServiceGroups)
                            .SelectMany(x => x.AdditionalServices)
                            .ToArray();

                        foreach (ProductDto gift in product.Gifts)
                        {
                            ProductAdditionalServiceDto additionalService = availAdditionalServices.FirstOrDefault(x => x.ProductId == gift.Id);

                            OrderProductDto giftOrderProduct = new OrderProductDto
                            {
                                Id = random.GetRandomId(),
                                ParentRecordId = copiedOrderProduct.Id,
                                Product = new ProductSimpleDto
                                {
                                    Id = gift.Id,
                                    Name = gift.Name,
                                    Weight = gift.Weight ?? 0,
                                    NameFullRu = gift.NameFullRu,
                                    TypeId = gift.TypeId,
                                    AdditionalServiceId = additionalService?.Id,
                                    Prices = product.Prices,
                                    BonusesToCharge = gift.BonusesToCharge
                                },
                                Quantity = orderProduct.Quantity,
                                StateId = OrderProductStatus.New.Id,
                                PriceOut = gift.Price,
                                CurrencyOutId = gift.CurrencyId,
                                Price = gift.Price,
                                PriceId = contractor.PriceTypeId,
                                CurrencyId = gift.CurrencyId,
                                AssemblyId = orderProduct.AssemblyId,
                                OrderFolderId = orderProduct.OrderFolderId.HasValue ? newOrderFolderIds[orderProduct.OrderFolderId.Value] : null,
                                AssemblyQuantity = orderProduct.AssemblyQuantity,
                                AssemblyIncluded = orderProduct.AssemblyIncluded,
                                IsGift = true,
                                IsAdditionalService = additionalService != null,
                                AdditionalWarranty = additionalService?.AdditionalWarranty ?? false,
                                Promo = promos.Data.FirstOrDefault(x => x.ProductIds.Contains(gift.Id)),
                            };

                            yield return giftOrderProduct;
                        }
                    }
                }
            }
        }

        private OrderViewModel CreateOrderViewModel()
        {
            return ServiceProvider.GetService<OrderViewModel>();
        }
    }
}