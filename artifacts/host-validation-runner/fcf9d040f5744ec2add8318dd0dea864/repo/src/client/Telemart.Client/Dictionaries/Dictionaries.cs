using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Telemart.Client.Data.Requests.Features.BonusType;
using Telemart.Client.Data.Requests.Features.Call;
using Telemart.Client.Data.Requests.Features.Carry;
using Telemart.Client.Data.Requests.Features.Category;
using Telemart.Client.Data.Requests.Features.Comment;
using Telemart.Client.Data.Requests.Features.Content;
using Telemart.Client.Data.Requests.Features.Entity;
using Telemart.Client.Data.Requests.Features.Invoice;
using Telemart.Client.Data.Requests.Features.Locations;
using Telemart.Client.Data.Requests.Features.Logistics;
using Telemart.Client.Data.Requests.Features.ModuleHelp;
using Telemart.Client.Data.Requests.Features.OrderDocuments;
using Telemart.Client.Data.Requests.Features.OrderSource;
using Telemart.Client.Data.Requests.Features.Payments;
using Telemart.Client.Data.Requests.Features.PosTerminal;
using Telemart.Client.Data.Requests.Features.Products;
using Telemart.Client.Data.Requests.Features.Purchase;
using Telemart.Client.Data.Requests.Features.Sms;
using Telemart.Client.Data.Requests.Features.TradeIn;
using Telemart.Client.Data.Requests.Features.Warranty;
using Telemart.Client.Data.Requests.Features.WorkPlace;
using Telemart.Client.Data.Requests.Features.WorkSchedule;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Extensions;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.Call;
using Telemart.Client.TransferObjects.Carry;
using Telemart.Client.TransferObjects.Comment;
using Telemart.Client.TransferObjects.Content;
using Telemart.Client.TransferObjects.Locations;
using Telemart.Client.TransferObjects.Payments;
using Telemart.Client.TransferObjects.PosTerminal;
using Telemart.Client.TransferObjects.TradeIn;
using Telemart.Client.TransferObjects.WorkSchedule;

namespace Telemart.Client.Dictionaries
{
    public sealed class Dictionaries : IDictionaries
    {
        private readonly Dictionary<Type, IReadOnlyCollection<DictionaryItemBase>> dictionaries = new Dictionary<Type, IReadOnlyCollection<DictionaryItemBase>>();

        public Dictionaries(IWebClient webClient)
        {
            WebClient = webClient;
        }

        private IWebClient WebClient { get; }

        public T GetItemById<T>(int id)
            where T : DictionaryItemBase
        {
            IReadOnlyCollection<T> items = GetItems<T>();
            T item = items.FirstOrDefault(x => x.Id == id);
            return item;
        }

        public T GetItemByName<T>(string name)
            where T : DictionaryItemBase
        {
            IReadOnlyCollection<T> items = GetItems<T>();
            T item = items.FirstOrDefault(x => x.Name == name);
            return item;
        }

        public IReadOnlyCollection<T> GetItems<T>()
            where T : DictionaryItemBase
        {
            IReadOnlyCollection<T> res;

            if (dictionaries.TryGetValue(typeof(T), out IReadOnlyCollection<DictionaryItemBase> collection))
            {
                res = (IReadOnlyCollection<T>)collection;
            }
            else
            {
                res = DictionaryItems<T>.Value;
            }

            return res;
        }

        public IEnumerable<Currency> GetCurrencies()
        {
            yield return Currency.Uah;
            yield return Currency.Usd;
            yield return Currency.Eur;
        }

        public OrderProductSource GetOrderProductSource(int sourceId, int? warehouseId, string sourceText, DateTime? sourceDate)
        {
            OrderProductSource source;

            switch (sourceId)
            {
                case OrderProductSourceType.NoneId:
                    source = new NoneOrderProductSource();
                    break;
                case OrderProductSourceType.WarehouseSourceId:
                    source = new WarehouseOrderProductSource(warehouseId!.Value, sourceText, sourceDate);
                    break;
                case OrderProductSourceType.PurchaseId:
                    source = new PurchaseOrderProductSource(warehouseId!.Value, sourceText, sourceDate);
                    break;
                case OrderProductSourceType.MovementId:
                    source = new MovementOrderProductSource(warehouseId!.Value, sourceText, sourceDate);
                    break;
                case OrderProductSourceType.NoProductId:
                    source = new NoProductOrderProductSource(sourceText);
                    break;
                case OrderProductSourceType.OtherId:
                    source = new OtherOrderProductSource(warehouseId!.Value, sourceText, sourceDate);
                    break;
                case OrderProductSourceType.LostId:
                    source = new LostOrderProductSource(sourceText);
                    break;
                case OrderProductSourceType.GuestId:
                    source = new GuestOrderProductSource(warehouseId!.Value, sourceText, sourceDate);
                    break;
                default:
                    throw new NotSupportedException($"SourceId:{sourceId} not supported.");
            }

            return source;
        }

        public IEnumerable<Payment> GetServiceRequestPaymentTypes(int orderPaymentId, IReadOnlyCollection<OrderPaymentDto> orderPayments, int orderContractorLimit = 0)
        {
            if (orderContractorLimit > 1)
            {
                yield return new Payment(-1, "На баланс", null, null, true, false, false, null, false, false, 0, null, 0, false);
            }

            if (orderPayments.Any(x => x.PaymentId == Payment.TerminalId))
            {
                foreach (Payment payment in GetRefundPayments(Payment.CashId, Payment.TerminalId))
                {
                    yield return payment;
                }

                yield break;
            }

            if (orderPaymentId == Payment.MonoPayId)
            {
                foreach (Payment payment in GetRefundPayments(Payment.CashId, Payment.MonoPayId).Where(x => x.Id != Payment.BankId))
                {
                    yield return payment;
                }

                yield break;
            }

            if (orderPaymentId == Payment.NovaPayId)
            {
                foreach (Payment payment in GetRefundPayments(Payment.CashId, Payment.NovaPayId).Where(x => x.Id != Payment.BankId))
                {
                    yield return payment;
                }

                yield break;
            }

            if (orderPaymentId == Payment.LiqPayId)
            {
                foreach (Payment payment in GetRefundPayments(Payment.CashId, Payment.LiqPayId).Where(x => x.Id != Payment.BankId))
                {
                    yield return payment;
                }

                yield break;
            }

            if (orderPaymentId == Payment.PortmoneId)
            {
                foreach (Payment payment in GetRefundPayments(Payment.CashId, Payment.PortmoneId).Where(x => x.Id != Payment.BankId))
                {
                    yield return payment;
                }

                yield break;
            }

            if (Payment.IsCachlessPayment(orderPaymentId) || Payment.IsCreditPayment(orderPaymentId))
            {
                yield return GetItemById<Payment>(orderPaymentId);
                yield break;
            }

            foreach (Payment payment in GetRefundPayments(Payment.CashId))
            {
                yield return payment;
            }
        }

        public IEnumerable<int> GetPaymentsBySubdivision(int subdivisionId)
        {
            if (subdivisionId == Subdivision.Telemart.Id || subdivisionId == Subdivision.Nofelet.Id)
            {
                foreach (Payment payment in GetItems<Payment>().Where(x => x.Id != Payment.NoId))
                {
                    yield return payment.Id;
                }
            }
            else if (subdivisionId == Subdivision.Retail.Id)
            {
                yield return Payment.CashId;
            }
            else if (subdivisionId == Subdivision.Wholesale.Id)
            {
                yield return Payment.CashId;
                yield return Payment.BankId;
                yield return Payment.NoId;
            }
            else
            {
                throw new NotSupportedException($"Subdivision with Id:{subdivisionId.ToString(CultureInfo.InvariantCulture)} not supported.");
            }
        }

        public IEnumerable<Payment> GetRefundPayments(int[] paymentIds, int? selectedPaymentId)
        {
            int[] refundPaymentIds = GetItems<Payment>().Where(x => paymentIds.Contains(x.Id))
                .SelectMany(x => x.RefundPaymentIds)
                .Distinct()
                .ToArray();

            return GetItems<Payment>()
                .Where(x => selectedPaymentId == x.Id || refundPaymentIds.Contains(x.Id));
        }

        public IEnumerable<Payment> GetRefundPayments(params int[] paymentIds) => GetRefundPayments(paymentIds, null);

        public Task LoadAsync()
        {
            return Task.WhenAll(
                QueryCarriesAsync(),
                QueryWorkPlaceTypesAsync(),
                QueryPaymentsAsync(),
                QueryWarrantiesAsync(),
                QueryBonusTypesAsync(),
                QuerySmsTemplatesAsync(),
                QueryInvoiceAdditionalCostSourcesAsync(),
                QueryProductPriceKindsAsync(),
                QueryReferralDiscountCodesAsync(),
                QuerySmsTypesAsync(),
                QueryProductPickupReasonsAsync(),
                QueryProductTypesAsync(),
                QueryCallDependencyTypesAsync(),
                QueryOrderSourcesAsync(),
                QueryNoProductReasonAsync(),
                QueryPosTypesAsync(),
                QueryCarryProvidersAsync(),
                QueryTradeInDocumentTypesAsync(),
                QueryOrderDocumentTypesAsync(),
                QueryModuleHelpUrlsAsync(),
                QueryAvatarsAsync(),
                QueryWorkScheduleTypeAsync(),
                QueryEntitiesAsync(),
                QueryExpireReasonsAsync(),
                QueryFeatureSettingsOptionsAsync(),
                QueryLocationTypesAsync());

            async Task QueryCarriesAsync()
            {
                List<CarryDto> records = await WebClient.ExecuteApiRequestAsync(new QueryCarries());

                dictionaries[typeof(CarryType)] = records
                    .Select(x => new CarryType(
                        GetItemById<CarryTypeKind>(x.CarryTypeId),
                        x.Id,
                        x.Name,
                        x.NameShort,
                        x.NameUa,
                        x.NameEn,
                        x.Position,
                        x.Active,
                        x.UseInMovements,
                        x.UseInOrder,
                        x.IsLocal,
                        x.RequireLastName,
                        x.RequireMiddleName,
                        x.TtnRegex,
                        x.DeliveryCost,
                        x.MinFreeDeliveryCost,
                        x.WeightLimit,
                        x.OrderCostLimit,
                        x.UseInServiceMovement,
                        x.CanSwitchInOrders,
                        x.StickerRequired,
                        x.AllowFreeUnderLimit,
                        x.CarryProviderId,
                        x.ScheduleDelivery,
                        x.Drivers,
                        x.InsurancePercent))
                    .ToArray();
            }

            async Task QueryProductPriceKindsAsync()
            {
                List<ProductPriceKindDto> records = await WebClient.ExecuteApiRequestAsync(new QueryProductPriceKinds());

                dictionaries[typeof(ProductPriceKind)] = records
                    .Select(x => new ProductPriceKind(
                        x.Id,
                        x.Name,
                        x.Configurator,
                        x.PriceColumn,
                        x.CurrencyId,
                        x.CanSwitchInOrders))
                    .ToArray();
            }

            async Task QueryReferralDiscountCodesAsync()
            {
                List<ReferralDiscountCodeDto> records = await WebClient.ExecuteApiRequestAsync(new QueryReferralDiscountCodes());

                dictionaries[typeof(ReferralDiscountCode)] = records.Select(x => new ReferralDiscountCode(x.Id, x.Name, x.Value)).ToArray();
            }

            async Task QueryCallDependencyTypesAsync()
            {
                List<CallDependencyTypeDto> records = await WebClient.ExecuteApiRequestAsync(new QueryCallDependencyTypes());

                dictionaries[typeof(CallDependencyType)] = records.Select(x => new CallDependencyType(x.Id, x.Name)).ToArray();
            }

            async Task QueryWorkPlaceTypesAsync()
            {
                List<WorkPlaceTypeDto> records = await WebClient.ExecuteApiRequestAsync(new QueryWorkPlaceTypes());

                dictionaries[typeof(WorkPlaceType)] = records.Where(x => !x.IsVirtual).Select(x => new WorkPlaceType(x.Id, x.Name)).ToArray();
            }

            async Task QueryEntitiesAsync()
            {
                List<EntityDto> records = await WebClient.ExecuteApiRequestAsync(new QueryEntities());

                dictionaries[typeof(Entity)] = records.Select(x => new Entity(x.Id, x.Name, x.DisplayName, x.DiscussionViewModels, x.Logistic)).ToArray();
            }

            async Task QueryInvoiceAdditionalCostSourcesAsync()
            {
                List<InvoiceAdditionalCostSourceDto> records = await WebClient.ExecuteApiRequestAsync(new QueryInvoiceAdditionalCostSources());

                dictionaries[typeof(InvoiceAdditionalCostSource)] = records
                    .Select(x => new InvoiceAdditionalCostSource(
                        x.Id,
                        x.Name))
                    .ToArray();
            }

            async Task QueryPaymentsAsync()
            {
                List<PaymentDto> records = await WebClient.ExecuteApiRequestAsync(new QueryPayments());

                dictionaries[typeof(Payment)] = records
                    .Select(x => new Payment(
                        x.Id,
                        x.Name,
                        x.NameUa,
                        x.NameEn,
                        x.Active,
                        x.Credit,
                        x.PartialCredit,
                        x.RefundPaymentIds,
                        x.AutoFillSources,
                        x.OnlyCreateOnWeb,
                        x.LimitUah,
                        x.MinLimitUah,
                        x.LimitUsd,
                        x.RefundRevertAllowed,
                        x.SmsTemplate,
                        x.RefundRequisitesControl,
                        x.CanEditProducts,
                        x.Fiscal))
                    .ToArray();
            }

            async Task QueryWarrantiesAsync()
            {
                List<WarrantyDto> records = await WebClient.ExecuteApiRequestAsync(new QueryWarranties()).GetPagedResultDataAsync();

                dictionaries[typeof(Warranty)] = records
                    .Select(x => new Warranty(x.Id, x.Name, x.NameUa, x.NameEn, x.ShortName, x.ShortNameUa, x.ShortNameEn, x.Weight, x.TradeInDefault))
                    .ToArray();
            }

            async Task QueryBonusTypesAsync()
            {
                List<BonusTypeDto> records = await WebClient.ExecuteApiRequestAsync(new QueryBonusTypes());

                dictionaries[typeof(BonusType)] = records
                    .Select(x => new BonusType(x.Id, x.Name, x.CashboxId, x.PaymentId, x.LifeTime))
                    .ToArray();
            }

            async Task QuerySmsTemplatesAsync()
            {
                List<SmsTemplateDto> records = await WebClient.ExecuteApiRequestAsync(new QuerySmsTemplates());

                dictionaries[typeof(SmsTemplate)] = records
                    .Select(x => new SmsTemplate(x.Id, x.Name, x.SmsText, x.ViberText, x.LegalEntityId, x.ShowInOrder, x.ShowInServiceRequest, x.Position))
                    .ToArray();
            }

            async Task QuerySmsTypesAsync()
            {
                List<SmsTypeDto> records = await WebClient.ExecuteApiRequestAsync(new QuerySmsTypes());

                dictionaries[typeof(SendMessageType)] = records
                    .Select(x => new SendMessageType(x.Id, x.Name))
                    .ToArray();
            }

            async Task QueryProductPickupReasonsAsync()
            {
                List<ProductPickupReasonDto> records = await WebClient.ExecuteApiRequestAsync(new QueryProductPickupReasons());

                dictionaries[typeof(ProductPickupReason)] = records
                    .Select(x => new ProductPickupReason(x.Id, x.Name))
                    .ToArray();
            }

            async Task QueryProductTypesAsync()
            {
                List<ProductTypeEntityDto> records = await WebClient.ExecuteApiRequestAsync(new QueryProductTypes());

                dictionaries[typeof(ProductType)] = records
                    .Select(x => new ProductType(x.Id, x.Name, x.IsVirtual, x.AllowedInAssembledComputerRule))
                    .ToArray();
            }

            async Task QueryOrderSourcesAsync()
            {
                List<OrderSourceDto> records = await WebClient.ExecuteApiRequestAsync(new QueryOrderSources());

                dictionaries[typeof(OrderSourceType)] = records
                    .Select(x => new OrderSourceType(x.Id, x.Name, x.Position, x.AvailOnClient, x.CanContactCustomer))
                    .ToArray();
            }

            async Task QueryNoProductReasonAsync()
            {
                List<NoProductReasonDto> reasons = await WebClient.ExecuteApiRequestAsync(new QueryNoProductReasons());

                dictionaries[typeof(NoProductReasonType)] = reasons
                    .Select(x => new NoProductReasonType(x.Id, x.Name))
                    .ToArray();
            }

            async Task QueryPosTypesAsync()
            {
                List<PosTypeDto> posTypeDtos = await WebClient.ExecuteApiRequestAsync(new QueryPosTypes());

                dictionaries[typeof(PosTerminalType)] = posTypeDtos
                    .Select(x => new PosTerminalType(x.Id, x.Name))
                    .ToArray();
            }

            async Task QueryCarryProvidersAsync()
            {
                List<CarryProviderDto> dtos = await WebClient.ExecuteApiRequestAsync(new QueryCarryProviders());

                dictionaries[typeof(CarryProvider)] = dtos
                    .Select(x => new CarryProvider(x.Id, x.Name, x.NameUkr, x.NameEn))
                    .ToArray();
            }

            async Task QueryTradeInDocumentTypesAsync()
            {
                List<TradeInDocumentTypeDto> records = await WebClient.ExecuteApiRequestAsync(new QueryTradeInDocumentTypes());

                dictionaries[typeof(TradeInDocumentType)] = records.Select(x => new TradeInDocumentType(x.Id, x.Name)).ToArray();
            }

            async Task QueryModuleHelpUrlsAsync()
            {
                List<ModuleHelpUrlDto> records = await WebClient.ExecuteApiRequestAsync(new QueryModuleHelpUrls());

                dictionaries[typeof(ModuleHelpUrl)] = records.Select(x => new ModuleHelpUrl(x.Id, x.ModuleView, x.ModuleName, x.TelewikiUrl)).ToArray();
            }

            async Task QueryAvatarsAsync()
            {
                List<AvatarDto> records = await WebClient.ExecuteApiRequestAsync(new QueryAvatars());

                dictionaries[typeof(AvatarType)] = records.Select(x => new AvatarType(x.Id, x.Name)).ToArray();
            }

            async Task QueryWorkScheduleTypeAsync()
            {
                List<WorkScheduleTypeDto> records = await WebClient.ExecuteApiRequestAsync(new QueryWorkScheduleTypes());

                dictionaries[typeof(WorkScheduleType)] = records.Select(x => new WorkScheduleType(x.Id, x.Name, x.ParentId)).ToArray();
            }

            async Task QueryExpireReasonsAsync()
            {
                List<ReasonExpireDto> reasons = await WebClient.ExecuteApiRequestAsync(new QueryExpireReasons());

                dictionaries[typeof(ExpireReasonType)] = reasons.Select(x => new ExpireReasonType(x.Id, x.Name, x.EntityId)).ToArray();
            }

            async Task QueryOrderDocumentTypesAsync()
            {
                List<OrderDocumentTypeDto> orderDocumentTypes = await WebClient.ExecuteApiRequestAsync(new QueryOrderDocumentTypes());

                dictionaries[typeof(OrderDocumentType)] = orderDocumentTypes.Select(x => new OrderDocumentType(x.Id, x.Name)).ToArray();
            }

            async Task QueryFeatureSettingsOptionsAsync()
            {
                List<FeatureOptionDto> featureSettingsOptionDtos = await WebClient.ExecuteApiRequestAsync(new QueryFeatureOptions());

                dictionaries[typeof(FeatureOption)] = featureSettingsOptionDtos
                    .Select(x => new FeatureOption(x.Id, x.Name))
                    .ToArray();
            }

            async Task QueryLocationTypesAsync()
            {
                List<LocationTypeEntityDto> records = await WebClient.ExecuteApiRequestAsync(new QueryLocationTypes());

                dictionaries[typeof(LocationType)] = records.Select(x => new LocationType(x.Id, x.Name)).ToArray();
            }
        }
    }
}