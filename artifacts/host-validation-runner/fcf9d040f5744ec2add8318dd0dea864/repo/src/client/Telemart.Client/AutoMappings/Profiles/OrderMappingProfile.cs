using System.Globalization;
using System.Linq;
using AutoMapper;
using Telemart.Client.AutoMappings.ValueResolvers;
using Telemart.Client.Business;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.Helpers;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.City;
using Telemart.Client.TransferObjects.Novaposhta;
using Telemart.Client.TransferObjects.PackList;
using Telemart.Client.TransferObjects.Warehouse;
using Telemart.Client.ViewModels.Money.Receive.BankPayment;
using Telemart.Client.ViewModels.Store;
using Telemart.Client.ViewModels.Store.CreateScanSheet;
using Telemart.Client.ViewModels.Store.Order;
using Telemart.Client.ViewModels.Store.Order.OrderDocuments;
using Telemart.Client.ViewModels.Store.Order.OrderEditPrice;
using Telemart.Client.ViewModels.Store.Order.PackList;

namespace Telemart.Client.AutoMappings.Profiles
{
    public sealed class OrderMappingProfile : Profile
    {
        public OrderMappingProfile()
        {
            CreateMap<OrderSimpleDto, OrderViewItem>()
                .ConstructUsing(x => OrderViewItem.Create())
                .ForMember(x => x.RealCompletedOnFiscalRegistrar, z => z.MapFrom(x => !string.IsNullOrWhiteSpace(x.FiscalId)))
                .ForMember(x => x.DontCall, x => x.MapFrom(z => z.Options.DontCall))
                .ForMember(x => x.SeparateWarrantyCards, x => x.MapFrom(z => z.Options.SeparateWarrantyCards))
                .ForMember(x => x.Subdivision, y => y.MapFrom<SubdivisionResolver, int>(z => z.SubdivisionId))
                .ForMember(x => x.Payment, y => y.MapFrom<DictionaryItemValueResolver<Payment>, int>(z => z.PaymentId))
                .ForMember(x => x.Carry, y => y.MapFrom<CarryTypeResolver, int>(z => z.CarryId))
                .ForMember(x => x.State, y => y.MapFrom<OrderStatusResolver, int>(z => z.StateId))
                .ForMember(x => x.LockEmployee, y => y.MapFrom(z => z.EmployeeLockName))
                .ForMember(x => x.TotalPrice, y => y.MapFrom(z => ResolvePrice(z)))
                .ForMember(x => x.ProductSourceInfo, y => y.MapFrom(z => ResolveProductSourceInfo(z)))
                .ForMember(x => x.CustomerStateText, x => x.Condition(z => !string.IsNullOrWhiteSpace(z.CustomerStateText)))
                .ForMember(x => x.Comment, y => y.MapFrom(z => OrderCommentHelper.GetJoinedComment(z.CustomerComment, z.EmployeeComment, z.SystemComment)))
                .ForMember(x => x.MoneyBackAmount, y => y.MapFrom(x => x.MoneyBackAmount))
                .AfterMap((source, target) =>
                {
                    foreach (OrderProductViewItem orderProductViewItem in target.Products)
                    {
                        orderProductViewItem.OrderWarehouseId = target.WarehouseId;
                        orderProductViewItem.OrderConfirmedBy = source.ConfirmedBy;
                    }
                });

            CreateMap<OrderProductSimpleDto, OrderProductViewItem>()
                .ForMember(x => x.CurrencyId, x => x.Ignore())
                .ForMember(x => x.AssemblyIncluded, x => x.Ignore())
                .ForMember(x => x.Price, x => x.Ignore())
                .ForMember(x => x.ProductId, x => x.Ignore())
                .ForMember(x => x.ShowAdditionalServiceIcon, y => y.MapFrom(z => z.IsAdditionalService && ProductType.IsAdditionalServiceProductType(z.ProductTypeId)))
                .ForMember(x => x.ShowAccessoryAdditionalServiceIcon, y => y.MapFrom(z => z.IsAdditionalService && ProductType.IsAccessoryAdditionalServiceProductType(z.ProductTypeId)))
                .ForMember(x => x.ProductPrefixRus, x => x.Ignore())
                .ForMember(x => x.ProductPrefixUkr, x => x.Ignore())
                .ForMember(x => x.ProductPrefixEn, x => x.Ignore())
                .ForMember(x => x.AssemblyId, x => x.Ignore())
                .ForMember(x => x.OrderFolderId, x => x.Ignore())
                .ForMember(x => x.AssemblyQuantity, x => x.Ignore())
                .ForMember(x => x.DeliveryDateTime, x => x.Ignore())
                .ForMember(x => x.OrderWarehouseId, x => x.Ignore())
                .ForMember(x => x.OrderConfirmedBy, x => x.Ignore())
                .ForMember(x => x.FreeDelivery, x => x.Ignore())
                .ForMember(x => x.State, y => y.MapFrom<OrderProductStatusResolver, int>(z => z.StateId))
                .ForMember(x => x.Source, y => y.MapFrom<OrderProductSourceResolver>())
                .ForMember(x => x.ProductName, x => x.MapFrom(z => z.NameFullRu))
                .ForMember(x => x.ProductNameUkr, x => x.MapFrom(z => z.NameFullUa))
                .ForMember(x => x.ProductNameEn, x => x.MapFrom(z => z.NameFullEn));

            CreateMap<OrderDto, OrderPackViewItem>()
                .ConstructUsing(x => OrderPackViewItem.Create())
                .ForMember(x => x.Subdivision, y => y.MapFrom<SubdivisionResolver, int>(z => z.SubdivisionId))
                .ForMember(x => x.Payment, y => y.MapFrom<DictionaryItemValueResolver<Payment>, int>(z => z.PaymentId))
                .ForMember(x => x.Carry, y => y.MapFrom<CarryTypeResolver, int>(z => z.CarryId))
                .ForMember(x => x.State, y => y.MapFrom<OrderStatusResolver, int>(z => z.StateId))
                .ForMember(x => x.LockEmployee, y => y.MapFrom(z => z.EmployeeLock == null ? string.Empty : z.EmployeeLock.Name))
                .ForMember(x => x.TotalPrice, y => y.MapFrom(z => ResolvePrice(z)))
                .ForMember(x => x.ProductSourceInfo, y => y.MapFrom(z => ResolveProductSourceInfo(z)))
                .ForMember(x => x.CustomerStateText, x => x.Condition(z => !string.IsNullOrWhiteSpace(z.CustomerStateText)))
                .ForMember(x => x.Comment, y => y.MapFrom(z => OrderCommentHelper.GetJoinedComment(z.CustomerComment, z.EmployeeComment, z.SystemComment)))
                .AfterMap((source, target) =>
                {
                    foreach (OrderProductViewItem orderProductViewItem in target.Products)
                    {
                        orderProductViewItem.OrderWarehouseId = target.WarehouseId;
                        orderProductViewItem.OrderConfirmedBy = source.ConfirmedBy;
                    }
                });

            CreateMap<OrderDto, OrderViewItem>()
                .ConstructUsing(x => OrderViewItem.Create())
                .ForMember(x => x.RealCompletedOnFiscalRegistrar, z => z.MapFrom(x => !string.IsNullOrWhiteSpace(x.FiscalId)))
                .ForMember(x => x.Preorder, x => x.Ignore())
                .ForMember(x => x.DontCall, x => x.MapFrom(z => z.Options.DontCall))
                .ForMember(x => x.SeparateWarrantyCards, x => x.MapFrom(z => z.Options.SeparateWarrantyCards))
                .ForMember(x => x.Subdivision, y => y.MapFrom<SubdivisionResolver, int>(z => z.SubdivisionId))
                .ForMember(x => x.Payment, y => y.MapFrom<DictionaryItemValueResolver<Payment>, int>(z => z.PaymentId))
                .ForMember(x => x.Carry, y => y.MapFrom<CarryTypeResolver, int>(z => z.CarryId))
                .ForMember(x => x.State, y => y.MapFrom<OrderStatusResolver, int>(z => z.StateId))
                .ForMember(x => x.LockEmployee, y => y.MapFrom(z => z.EmployeeLock == null ? string.Empty : z.EmployeeLock.Name))
                .ForMember(x => x.TotalPrice, y => y.MapFrom(z => ResolvePrice(z)))
                .ForMember(x => x.ProductSourceInfo, y => y.MapFrom(z => ResolveProductSourceInfo(z)))
                .ForMember(x => x.CustomerStateText, x => x.Condition(z => !string.IsNullOrWhiteSpace(z.CustomerStateText)))
                .ForMember(x => x.Comment, y => y.MapFrom(z => OrderCommentHelper.GetJoinedComment(z.CustomerComment, z.EmployeeComment, z.SystemComment)))
                .AfterMap((source, target) =>
                {
                    foreach (OrderProductViewItem orderProductViewItem in target.Products)
                    {
                        orderProductViewItem.OrderWarehouseId = target.WarehouseId;
                        orderProductViewItem.OrderConfirmedBy = source.ConfirmedBy;
                    }
                });

            CreateMap<OrderDto, OrderEditPriceViewItem>()
                .ForMember(x => x.Subdivision, y => y.MapFrom<SubdivisionResolver, int>(z => z.SubdivisionId))
                .ForMember(x => x.BonusesQuantity, y => y.MapFrom(z => z.Bonuses.Sum(x => x.Quantity)))
                .ForMember(x => x.BonusesToChargeQuantity, y => y.MapFrom(z => z.Products.Sum(q => (q.BonusesToCharge ?? 0) * q.Quantity)))
                .ForMember(x => x.OrderProducts, y => y.MapFrom(z => z.Products))
                .ForMember(x => x.MoneyBackAmount, y => y.MapFrom(z => z.MoneyBackAmount));

            CreateMap<OrderDto, ScanSheetOrderViewItem>()
                .ForMember(x => x.Subdivision, y => y.MapFrom<SubdivisionResolver, int>(z => z.SubdivisionId))
                .ForMember(x => x.Carry, y => y.MapFrom<CarryTypeResolver, int>(z => z.CarryId))
                .ForMember(x => x.PriceToCost, y => y.MapFrom(z => ResolvePriceToCost(z)))
                .ForMember(x => x.Comment, y => y.MapFrom(z => OrderCommentHelper.GetJoinedComment(z.CustomerComment, z.EmployeeComment, z.SystemComment).Replace("\r\n", " ").Replace("\n", " ")));

            CreateMap<OrderDto, BankPaymentConfirmOrderViewItem>()
                .ForMember(x => x.Comment, x => x.MapFrom(z => z.EmployeeComment))
                .ForMember(x => x.State, y => y.MapFrom<OrderStatusResolver, int>(z => z.StateId))
                .ForMember(x => x.Payment, y => y.MapFrom<DictionaryItemValueResolver<Payment>, int>(z => z.PaymentId))
                .ForMember(x => x.Prices, y => y.MapFrom(z => z.GetPrices()));

            CreateMap<OrderProductDto, OrderProductViewItem>()
                .ForMember(x => x.DeliveryDateTime, x => x.Ignore())
                .ForMember(x => x.OrderWarehouseId, x => x.Ignore())
                .ForMember(x => x.OrderConfirmedBy, x => x.Ignore())
                .ForMember(x => x.ProductId, y => y.MapFrom(z => z.Product.Id))
                .ForMember(x => x.ProductName, y => y.MapFrom(z => z.Product.Name))
                .ForMember(x => x.ProductTypeId, y => y.MapFrom(z => z.Product.TypeId))
                .ForMember(x => x.State, y => y.MapFrom<OrderProductStatusResolver, int>(z => z.StateId))
                .ForMember(x => x.Source, y => y.MapFrom<OrderProductSourceResolver>())
                .ForMember(x => x.Price, y => y.MapFrom(z => z.Price))
                .ForMember(x => x.ShowAdditionalServiceIcon, y => y.MapFrom(z => z.IsAdditionalService && ProductType.IsAdditionalServiceProductType(z.Product.TypeId)))
                .ForMember(x => x.ShowAccessoryAdditionalServiceIcon, y => y.MapFrom(z => z.IsAdditionalService && ProductType.IsAccessoryAdditionalServiceProductType(z.Product.TypeId)))
                .ForMember(x => x.CurrencyId, y => y.MapFrom(z => z.CurrencyId));

            CreateMap<OrderProductDto, OrderProductSaveDto>()
                .ForMember(x => x.InitiatedBy, x => x.Ignore());

            CreateMap<OrderProductDto, OrderEditPriceProductViewItem>()
                .ForMember(x => x.NewPrice, x => x.Ignore())
                .ForMember(x => x.ProductId, y => y.MapFrom(z => z.Product.Id))
                .ForMember(x => x.ProductName, y => y.MapFrom(z => z.Product.Name))
                .ForMember(x => x.ProductTypeId, y => y.MapFrom(z => z.Product.TypeId))
                .ForMember(x => x.State, y => y.MapFrom<OrderProductStatusResolver, int>(z => z.StateId))
                .ForMember(x => x.Source, y => y.MapFrom<OrderProductSourceResolver>())
                .ForMember(x => x.Price, y => y.MapFrom(z => z.Price))
                .ForMember(x => x.CurrencyId, y => y.MapFrom(z => z.CurrencyId));

            CreateMap<NpWarehouseDto, NewPostWarehouseViewItem>()
                .ForMember(x => x.Ref, y => y.MapFrom(z => z.Ref))
                .ForMember(x => x.Description, y => y.MapFrom(z => z.Address))
                .ForMember(x => x.Number, y => y.MapFrom(z => z.Number))
                .ForMember(x => x.TotalMaxWeightAllowed, y => y.MapFrom(z => z.TotalMaxWeightAllowed.Equals(0) ? "∞" : $"{z.TotalMaxWeightAllowed} кг"));

            CreateMap<ContractorDto, OrderContractorViewItem>()
                .ForMember(x => x.Valid, x => x.Ignore())
                .ForMember(x => x.Subdivision, y => y.MapFrom<SubdivisionResolver, int>(z => z.SubdivisionId))
                .ForMember(x => x.EmployeeId, y => y.Condition(z => z.EmployeeId > 0));

            CreateMap<Payment, OrderPaymentViewItem>()
                .ForMember(x => x.Id, y => y.MapFrom(z => z.Id))
                .ForMember(x => x.Name, y => y.MapFrom(z => z.Name))
                .ForMember(x => x.Valid, y => y.Ignore());
            CreateMap<CityDto, OrderCityViewItem>()
                .ForMember(x => x.Id, y => y.MapFrom(z => z.Id))
                .ForMember(x => x.Name, y => y.MapFrom(z => z.Name))
                .ForMember(x => x.CityCarries, x => x.MapFrom(z => z.CityCarries.Select(q => q.CarryId)))
                .ForMember(x => x.Valid, y => y.Ignore());
            CreateMap<CarryType, OrderCarryTypeViewItem>()
                .ForMember(x => x.Id, y => y.MapFrom(z => z.Id))
                .ForMember(x => x.Name, y => y.MapFrom(z => z.Name))
                .ForMember(x => x.Position, y => y.MapFrom(z => z.Position))
                .ForMember(x => x.IsLocal, y => y.MapFrom(z => z.IsLocal))
                .ForMember(x => x.Active, y => y.MapFrom(z => z.Active))
                .ForMember(x => x.RequireLastName, y => y.MapFrom(z => z.RequireLastName))
                .ForMember(x => x.RequireMiddleName, y => y.MapFrom(z => z.RequireMiddleName))
                .ForMember(x => x.Valid, y => y.Ignore());
            CreateMap<WarehouseDto, OrderWarehouseViewItem>()
                .ForMember(x => x.Id, y => y.MapFrom(z => z.Id))
                .ForMember(x => x.AssemblyWarehouseId, y => y.MapFrom(z => z.AssemblyWarehouseId))
                .ForMember(x => x.Name, y => y.MapFrom(z => z.Name))
                .ForMember(x => x.CityId, y => y.MapFrom(z => z.CityId))
                .ForMember(x => x.Position, y => y.MapFrom(z => z.Position))
                .ForMember(x => x.Active, y => y.MapFrom(z => z.Active == 1))
                .ForMember(x => x.Valid, y => y.Ignore());

            CreateMap<OrderPaymentDto, OrderPaymentRecordViewItem>()
                .ConstructUsing(x => OrderPaymentRecordViewItem.Create())
                .ForMember(x => x.RealCompletedOnFiscalRegistrar, z => z.MapFrom(x => !string.IsNullOrWhiteSpace(x.FiscalId)))
                .ForMember(x => x.Currency, y => y.MapFrom<CurrencyTypeResolver, int>(z => z.CurrencyId))
                .ForMember(x => x.State, y => y.MapFrom<DictionaryItemValueResolver<RefundState>, int>(z => z.RefundStateId ?? 0))
                .ForMember(x => x.Payment, y => y.MapFrom<DictionaryItemValueResolver<Payment>, int>(z => z.PaymentId ?? 0))
                .ForMember(x => x.ReceivedOn, y => y.MapFrom(z => z.ReceivedOn))
                .ForMember(x => x.Comment, y => y.MapFrom(z => z.Comment));

            CreateMap<ExternalPaymentDto, ExternalPaymentViewItem>()
                .ForMember(x => x.Payment, y => y.MapFrom<DictionaryItemValueResolver<Payment>, int>(z => z.PaymentId))
                .ForMember(x => x.ParentPayment, y => y.MapFrom<DictionaryItemValueResolver<Payment>, int>(z => z.ParentPaymentId ?? 0));

            CreateMap<ClientContactDto, ClientContactViewItem>()
                .ForMember(x => x.Type, y => y.MapFrom<DictionaryItemValueResolver<ClientContactType>, int>(z => z.Type))
                .ForMember(x => x.CallState, y => y.MapFrom<DictionaryItemValueResolver<CallState>, int>(z => z.CallState ?? 0))
                .ForMember(x => x.EmailState, y => y.MapFrom<DictionaryItemValueResolver<EmailState>, int>(z => z.Type == ClientContactType.Email.Id ? EmailState.Send.Id : 0))
                .ForMember(x => x.SmsState, y => y.MapFrom<DictionaryItemValueResolver<SmsState>, int>(z => z.SmsState ?? 0));

            CreateMap<PackListDto, PackListViewItem>()
                .ForMember(x => x.RecognizeBarcodeViewModelVisible, x => x.Ignore());
            CreateMap<PackListOrderDto, PackListOrderViewItem>()
                .ForMember(x => x.OrderState, y => y.MapFrom<OrderStatusResolver, int>(z => z.OrderStateId));

            CreateMap<OrderCellDto, OrderCellViewItem>();
            CreateMap<OrderBillDto, OrderBillViewItem>()
                .ForMember(x => x.Payment, y => y.MapFrom<DictionaryItemValueResolver<Payment>, int>(z => z.PaymentId));

            CreateMap<OrderSourceType, OrderSourceTypeViewItem>()
                .ForMember(x => x.AvailOn, x => x.MapFrom(y => y.AvailOnClient))
                .ForMember(x => x.Valid, x => x.Ignore());

            CreateMap<OrderDocumentDto, OrderDocumentViewItem>();
            CreateMap<OrderDocumentSimpleDto, OrderDocumentViewItem>()
                .ForMember(x => x.Data, x => x.Ignore());
        }

        private static Prices ResolvePrice(OrderDto source)
        {
            return source.GetTotalAmount() + source.GetDeliveryCostAmount();
        }

        private static Prices ResolvePrice(OrderSimpleDto source)
        {
            return source.GetTotalAmount() + source.GetDeliveryCostAmount();
        }

        private static Prices ResolvePriceToCost(OrderDto source)
        {
            return source.GetTotalAmount() + source.GetDeliveryCostAmount() - source.GetPayedAmount();
        }

        private static string ResolveProductSourceInfo(OrderDto source)
        {
            int count = 0;
            int totalCount = 0;

            if (source.Products != null)
            {
                foreach (OrderProductDto orderProduct in source.Products.Where(x => x.StateId == OrderProductStatus.Clarify.Id))
                {
                    if (orderProduct.SourceId > 0)
                    {
                        count++;
                    }

                    totalCount++;
                }
            }

            return $"{count.ToString(CultureInfo.CurrentUICulture)}/{totalCount.ToString(CultureInfo.CurrentUICulture)}";
        }

        private static string ResolveProductSourceInfo(OrderSimpleDto source)
        {
            int count = 0;
            int totalCount = 0;

            if (source.Products != null)
            {
                foreach (OrderProductSimpleDto orderProduct in source.Products.Where(x => x.StateId == OrderProductStatus.Clarify.Id))
                {
                    if (orderProduct.SourceId > 0)
                    {
                        count++;
                    }

                    totalCount++;
                }
            }

            return $"{count.ToString(CultureInfo.CurrentUICulture)}/{totalCount.ToString(CultureInfo.CurrentUICulture)}";
        }
    }
}