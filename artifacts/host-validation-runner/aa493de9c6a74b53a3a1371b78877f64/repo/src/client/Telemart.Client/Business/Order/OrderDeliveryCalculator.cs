using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Telemart.Client.Common;
using Telemart.Client.Data.Requests.Features.Order.Actions;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Business.Order
{
    public sealed class OrderDeliveryCalculator : IOrderDeliveryCalculator
    {
        public OrderDeliveryCalculator(IWebClient webClient, IOrderRules orderRules)
        {
            WebClient = webClient ?? throw new ArgumentNullException(nameof(webClient));
            OrderRules = orderRules ?? throw new ArgumentNullException(nameof(orderRules));
        }

        private IWebClient WebClient { get; }

        private IOrderRules OrderRules { get; }

        public async Task<OrderDeliveryTime> CalculateOrderDeliveryTimeAsync(
            int orderId,
            int? warehouseId,
            int? assemblyWarehouseId,
            int? bufferWarehouseId,
            int? additionalServiceWarehouseId,
            int carryTypeId,
            int subdivisionId,
            OrderStatus orderState,
            IReadOnlyCollection<IOrderProduct> orderProducts,
            IReadOnlyCollection<OrderFolderDto> folders)
        {
            if (warehouseId == null)
            {
                return new OrderDeliveryTime(
                    null,
                    null,
                    null,
                    null,
                    new Dictionary<int, DateTime>(),
                    null,
                    null);
            }

            OrderProductLogisticsDto[] orderProductLogisticsDtos = orderProducts
                .Where(x => OrderRules.NeedCalcProductDateX(orderState, x))
                .Select(x => Map(x, folders, orderProducts))
                .ToArray();

            Dictionary<int, DateTime> deliveryDates;

            OrderLogisticsResultDto result = null;

            if (orderProductLogisticsDtos.Any())
            {
                OrderLogisticsDto dto = new OrderLogisticsDto(
                    orderId,
                    warehouseId.Value,
                    assemblyWarehouseId,
                    bufferWarehouseId,
                    additionalServiceWarehouseId,
                    carryTypeId,
                    subdivisionId,
                    orderProductLogisticsDtos);

                result = await WebClient.ExecuteApiRequestAsync(new CalculateLogistics(dto));

                deliveryDates = result.OrderProducts.ToDictionary(x => x.Id, x => x.DeliveryTime);
            }
            else
            {
                deliveryDates = new Dictionary<int, DateTime>();
            }

            return new OrderDeliveryTime(
                result?.DeliveryTime,
                result?.DeliveryTimeTo,
                result?.AssemblyDates,
                result?.AdditionalServiceDates,
                deliveryDates,
                result?.TotalAdditionalServiceEstimate,
                result?.AdditionalServiceQuotas);
        }

        private static OrderProductLogisticsDto Map(IOrderProduct orderProduct, IReadOnlyCollection<OrderFolderDto> folders, IReadOnlyCollection<IOrderProduct> orderProducts)
        {
            OrderFolderDto orderFolder = orderProducts.Any(x => x.OrderFolderId == orderProduct.OrderFolderId && x.ProductId == Constants.AssemblyServiceProductId)
                ? folders.FirstOrDefault(x => orderProduct.OrderFolderId == x.Id
                && (x.TypeId == OrderFolderType.AssemblyServiceId || x.TypeId == OrderFolderType.AssembledComputerRuleId))
                : null;

            return new OrderProductLogisticsDto(
                orderProduct.Id,
                orderProduct.State == OrderProductStatus.Clarify && orderProduct.Source is WarehouseOrderProductSource ? DateTime.Now : orderProduct.Source.SourceDate ?? DateTime.Now,
                orderProduct.Source.WarehouseId,
                orderFolder?.Quantity,
                orderFolder?.Id,
                orderProduct.ParentRecordId,
                orderProduct.IsAdditionalService,
                orderProduct.AssemblyIncluded,
                orderProduct.Quantity,
                null,
                orderProduct.ProductId,
                orderProduct.ProductName,
                orderProduct.Source?.Id == OrderProductSourceType.GuestId);
        }
    }
}