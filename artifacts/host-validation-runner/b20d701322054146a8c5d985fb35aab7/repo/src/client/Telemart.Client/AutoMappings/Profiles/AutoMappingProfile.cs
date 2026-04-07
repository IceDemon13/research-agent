using System;
using System.Linq;
using AutoMapper;
using Telemart.Client.AutoMappings.ValueResolvers;
using Telemart.Client.Data.Requests.Features.Catalog.TransferObjects;
using Telemart.Client.Dictionaries;
using Telemart.Client.Mediator.Requests;
using Telemart.Client.PosTerminal.Ingenico;
using Telemart.Client.Reports.AdditionalServiceProduct;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.AssemblyFullRule;
using Telemart.Client.TransferObjects.AutoSource;
using Telemart.Client.TransferObjects.Call;
using Telemart.Client.TransferObjects.Carry;
using Telemart.Client.TransferObjects.City;
using Telemart.Client.TransferObjects.CompanyStructure;
using Telemart.Client.TransferObjects.Customer;
using Telemart.Client.TransferObjects.Discussions;
using Telemart.Client.TransferObjects.InvoiceBudget;
using Telemart.Client.TransferObjects.Notification;
using Telemart.Client.TransferObjects.Novaposhta;
using Telemart.Client.TransferObjects.ParserRequests;
using Telemart.Client.TransferObjects.ParserSearchTemplate;
using Telemart.Client.TransferObjects.Payments;
using Telemart.Client.TransferObjects.Quotas;
using Telemart.Client.TransferObjects.RobotProperty;
using Telemart.Client.TransferObjects.Showcase;
using Telemart.Client.TransferObjects.SupplierCurrency;
using Telemart.Client.TransferObjects.Task;
using Telemart.Client.TransferObjects.Terminal;
using Telemart.Client.TransferObjects.TradeIn;
using Telemart.Client.TransferObjects.WorkSchedule;
using Telemart.Client.ViewModels.Area;
using Telemart.Client.ViewModels.AssembledComputerRule;
using Telemart.Client.ViewModels.AssemblyFullRule;
using Telemart.Client.ViewModels.Carry;
using Telemart.Client.ViewModels.Cities;
using Telemart.Client.ViewModels.Common;
using Telemart.Client.ViewModels.CompanyStructure;
using Telemart.Client.ViewModels.Content.ProductFeatureGroups;
using Telemart.Client.ViewModels.Customer;
using Telemart.Client.ViewModels.Discussions;
using Telemart.Client.ViewModels.District;
using Telemart.Client.ViewModels.History.Phone;
using Telemart.Client.ViewModels.Layouts;
using Telemart.Client.ViewModels.Notification;
using Telemart.Client.ViewModels.Parser.Dictionary;
using Telemart.Client.ViewModels.Payments;
using Telemart.Client.ViewModels.Quotas;
using Telemart.Client.ViewModels.RobotProperties;
using Telemart.Client.ViewModels.Showcase;
using Telemart.Client.ViewModels.Store.Call;
using Telemart.Client.ViewModels.Store.CreateScanSheetForEntities;
using Telemart.Client.ViewModels.Store.Invoice;
using Telemart.Client.ViewModels.Store.Order;
using Telemart.Client.ViewModels.Store.Order.AutoSource;
using Telemart.Client.ViewModels.Store.Order.CreateCompleted;
using Telemart.Client.ViewModels.SupplierCurrency;
using Telemart.Client.ViewModels.Tasks;
using Telemart.Client.ViewModels.TradeIn;
using Telemart.Client.ViewModels.WorkSchedule;
using Telemart.Common.TransferObjects;

namespace Telemart.Client.AutoMappings.Profiles
{
    public class AutoMappingProfile : Profile
    {
        public AutoMappingProfile()
        {
            CreateMap<PhoneHistoryDto, PhoneHistoryViewItem>()
                .ForMember(x => x.State, y => y.MapFrom<PhoneHistoryStateValueResolver, int>(z => z.StateId));
            CreateMap<CityDto, CityViewItem>()
                .ForMember(x => x.UpCityIdStr, x => x.Ignore())
                .ForMember(x => x.UklonCityIdStr, x => x.Ignore());
            CreateMap<CityCarryDto, CityCarryViewItem>();
            CreateMap<AreaDto, AreaViewItem>()
                .ForMember(x => x.EmployeeLockId, x => x.Ignore())
                .ForMember(x => x.EmployeeLockName, x => x.Ignore());

            CreateMap<CarryDto, CarryViewItem>();
            CreateMap<CarryViewItem, CarrySaveDto>();
            CreateMap<ModuleLayoutDto, ModuleLayoutViewItem>();

            CreateMap<PaymentDto, PaymentViewItem>();

            CreateMap<CallDependencyDto, CallDependencyViewItem>();

            CreateMap<AssemblyFullRuleDto, AssemblyFullRuleViewItem>()
                .ForMember(x => x.Operation, y => y.MapFrom<DictionaryItemValueResolver<AssemblyFullRuleOperation>, int>(z => z.OperationId ?? 0));

            CreateMap<AssemblyFullRuleParameter, AssemblyFullRuleViewItem>(MemberList.Source)
                .ForSourceMember(x => x.IsNew, x => x.DoNotValidate());

            CreateMap<ShowcaseDto, ShowcaseViewItem>();
            CreateMap<ShowcaseParameter, ShowcaseViewItem>(MemberList.Source)
                .ForSourceMember(x => x.IsNew, x => x.DoNotValidate());

            CreateMap<ShowcaseCategoryDto, ShowcaseCategoryViewItem>()
                .ForMember(x => x.Quantity, y => y.MapFrom(z => z.Quantity))
                .ForMember(x => x.QuantityOld, y => y.MapFrom(z => z.Quantity))
                .ForMember(x => x.CategoryId, y => y.MapFrom(z => z.CategoryId))
                .ForMember(x => x.CategoryOldId, y => y.MapFrom(z => z.CategoryId))
                .ForMember(x => x.WarehouseId, y => y.MapFrom(z => z.WarehouseId))
                .ForMember(x => x.WarehouseOldId, y => y.MapFrom(z => z.WarehouseId))
                .ForMember(x => x.CountLocations, y => y.Ignore())
                .ForMember(x => x.QuantityClusterFact, y => y.Ignore())
                .ForMember(x => x.QuantityClusterPlan, y => y.Ignore())
                .ForMember(x => x.DifferenceCluster, y => y.Ignore())
                .ForMember(x => x.QuantityLocationFact, y => y.Ignore())
                .ForMember(x => x.AverageQuantityLocation, y => y.Ignore())
                .ForMember(x => x.IsChanged, y => y.Ignore());

            CreateMap<CreateCompletedOrderParameter, CreateCompletedOrderViewItem>(MemberList.Source)
                .ForSourceMember(x => x.ContractorTemplateId, x => x.DoNotValidate());

            CreateMap<ShowcaseClusterDto, ShowcaseClusterViewItem>()
                .ForMember(x => x.QuantityClusterPlan, y => y.Ignore())
                .ForMember(x => x.CountLocations, y => y.Ignore());

            CreateMap<ShowcaseClusterViewItem, ShowcaseClusterSaveDto>();

            CreateMap<CustomerDto, CustomerViewItem>();
            CreateMap<CustomerBonusDto, CustomerBonusViewItem>();

            CreateMap<DistrictDto, DistrictViewItem>()
                .ForMember(x => x.EmployeeLockId, x => x.Ignore())
                .ForMember(x => x.EmployeeLockName, x => x.Ignore());

            CreateMap<CustomerAssemblyDto, CustomerAssemblyViewItem>();

            CreateMap<PickupProductDto, PickupProductViewItem>()
                .ForMember(x => x.Name, y => y.MapFrom(z => z.ProductName))
                .ForMember(x => x.NameUkr, y => y.MapFrom(z => z.ProductNameUa))
                .ForMember(x => x.NameEn, y => y.MapFrom(z => z.ProductNameEn))
                .ForMember(x => x.Processed, x => x.Ignore())
                .ForMember(x => x.ProductName, y => y.Ignore());

            CreateMap<PickupProductViewItem, PickupProductDto>()
                .ForMember(x => x.ProductName, y => y.MapFrom(z => z.Name))
                .ForMember(x => x.ProductNameUa, y => y.MapFrom(z => z.NameUkr))
                .ForMember(x => x.ProductNameEn, y => y.MapFrom(z => z.NameEn))
                .ForMember(x => x.ReasonIds, x => x.MapFrom(z => z.ReasonIds.Cast<int>().ToList()));

            CreateMap<OrderAutoConfirmSettingDto, OrderAutoConfirmSettingViewItem>()
                .ForMember(x => x.Payment, y => y.MapFrom<DictionaryItemValueResolver<Payment>, int>(z => z.PaymentId))
                .ForMember(x => x.Carry, y => y.MapFrom<DictionaryItemValueResolver<CarryType>, int>(z => z.CarryId));

            CreateMap<OrderGeneralConfirmSettingDto, OrderGeneralConfirmSettingViewItem>()
                .ForMember(x => x.IntValue, y => y.Ignore())
                .ForMember(x => x.BoolValue, y => y.Ignore())
                .ForMember(x => x.ActiveOld, y => y.MapFrom(x => x.Active))
                .ForMember(x => x.ValueOld, y => y.MapFrom(x => x.Value));

            CreateMap<DistrictParameter, DistrictViewItem>(MemberList.Source)
                .ForSourceMember(x => x.IsNew, x => x.DoNotValidate());

            CreateMap<ParserSearchTemplateFeatureDto, ParserSearchTemplateFeatureViewItem>();

            CreateMap<ParserSearchTemplateDto, ParserSearchTemplateViewItem>();

            CreateMap<ProductSearchTemplateDto, ProductSearchTemplateViewItem>();
            CreateMap<ProductFeatureValueSearchTemplateDto, ProductFeatureValueSearchTemplateViewItem>();

            CreateMap<FeatureContractorParserSourceDto, FeatureContractorParserSourceViewItem>()
                .ForMember(x => x.FeatureName, x => x.MapFrom(z => $"{z.FeatureName} ({z.FeatureId})"))
                .ForMember(x => x.AllowedPriorities, x => x.Ignore());

            CreateMap<CustomerBonusLogDto, CustomerBonusLogViewItem>()
                .ForMember(x => x.DocumentId, x => x.Ignore())
                .ForMember(x => x.BonusType, x => x.MapFrom<DictionaryItemValueResolver<BonusType>, int>(z => z.BonusTypeId));

            CreateMap<SaveProductsRequest, SaveProductsMessage>();
            CreateMap<ClearAvailableBySupplierWarehouseIdRequest, ClearAvailableMessage>();
            CreateMap<ProductSaveDto, ProductMessage>()
                .ForMember(x => x.Barcode, x => x.Ignore())
                .ForMember(x => x.Link, x => x.Ignore())
                .ForMember(x => x.Tnved, x => x.Ignore())
                .ForMember(x => x.Category, x => x.Ignore())
                .ForMember(x => x.FullCategory, x => x.Ignore());

            CreateMap<ParserPriceDto, PriceMessage>();
            CreateMap<ComplectationGeneratorOptionsDto, ComplactationGeneratorOptionsItem>()
                .ForMember(x => x.OverPricePercent, x => x.Ignore());
            CreateMap<SupplierCurrencyRateHistoryDto, SupplierCurrencyItem>();

            CreateMap<SupplierWarehouseAvailDto, SupplierWarehouseAvailMessage>()
                .ForMember(x => x.AvailType, x => x.Ignore())
                .ForMember(x => x.Quantity, x => x.Ignore());

            CreateMap<RobotPropertyDto, RobotPropertyViewItem>()
                .ForMember(x => x.ValuesText, x => x.Ignore())
                .ForMember(x => x.EmployeeLockId, x => x.Ignore())
                .ForMember(x => x.EmployeeLockName, x => x.Ignore());
            CreateMap<RobotPropertyViewItem, RobotPropertyDto>();
            CreateMap<RobotPropertyValueDto, RobotPropertyValueViewItem>();
            CreateMap<RobotPropertyValueViewItem, RobotPropertyValueDto>();
            CreateMap<RobotPropertyParameter, RobotPropertyViewItem>(MemberList.Source)
                .ForSourceMember(x => x.GroupNames, x => x.DoNotValidate())
                .ForSourceMember(x => x.IsNew, x => x.DoNotValidate());

            CreateMap<AutoSourceSettingDto, AutoSourceSettingItem>();

            CreateMap<TradeInDto, TradeInViewItem>()
                .ForMember(x => x.Name, y => y.MapFrom(z => z.ProductName))
                .ForMember(x => x.NameUkr, y => y.MapFrom(z => z.ProductNameUa))
                .ForMember(x => x.NameEn, y => y.MapFrom(z => z.ProductNameEn))
                .ForMember(x => x.IsDocumentTypeChanged, x => x.Ignore())
                .ForMember(x => x.CarryTypeOut, y => y.MapFrom<DictionaryItemValueResolver<CarryType>, int>(z => z.CarryOutId ?? 0))
                .ForMember(x => x.CarryTypeIn, y => y.MapFrom<DictionaryItemValueResolver<CarryType>, int>(z => z.CarryInId ?? 0))
                .ForMember(x => x.ProductName, y => y.Ignore())
                .ForMember(x => x.NeedSerialNumber, y => y.Ignore())
                .ForMember(x => x.ComplaintsCountString, y => y.Ignore());
            CreateMap<TradeInDocumentDto, TradeInDocumentViewItem>();
            CreateMap<TradeInDocumentSimpleDto, TradeInDocumentViewItem>()
                .ForMember(x => x.Data, x => x.Ignore());
            CreateMap<TradeInCancelReasonDto, TradeInReasonCancelTypeViewItem>();
            CreateMap<CallNotificationDto, CallNotificationRequest>();
            CreateMap<TradeInEDocumentSimpleDto, TradeInEDocumentViewItem>()
                .ForMember(x => x.Sent, y => y.Ignore());

            CreateMap<NotificationDto, NotificationViewItem>();

            CreateMap<DepartmentDto, DepartmentViewItem>()
                .ForMember(x => x.ParentDepartmentId, y => y.MapFrom(a => a.ParentId))
                .ForMember(x => x.EmployeeLockId, x => x.Ignore())
                .ForMember(x => x.EmployeeLockName, x => x.Ignore());

            CreateMap<QuotaDto, QuotaViewItem>();
            CreateMap<RequisitesViewItem, RefundRequisitesDto>();
            CreateMap<RefundRequisitesDto, RequisitesViewItem>()
                .ForMember(x => x.Visible, x => x.Ignore())
                .ForMember(x => x.Enabled, x => x.Ignore())
                .ForMember(x => x.Required, x => x.Ignore());
            CreateMap<WorkScheduleDto, WorkScheduleViewItem>()
                .ForMember(x => x.OldStart, y => y.MapFrom(x => x.Start))
                .ForMember(x => x.OldEnd, y => y.MapFrom(x => x.End))
                .ForMember(x => x.OldDays, y => y.MapFrom(x => x.Days))
                .ForMember(x => x.OldName, y => y.MapFrom(z => z.Name))
                .ForMember(x => x.TypeName, y => y.Ignore())
                .ForMember(x => x.ParentTypeName, y => y.Ignore());

            CreateMap<HolidayDto, HolidayViewItem>()
                .ForMember(x => x.OldStart, y => y.MapFrom(x => x.Start))
                .ForMember(x => x.OldEnd, y => y.MapFrom(x => x.End))
                .ForMember(x => x.OldDate, y => y.MapFrom(x => x.Date))
                .ForMember(x => x.TypeName, y => y.Ignore())
                .ForMember(x => x.ParentTypeName, y => y.Ignore());
            CreateMap<DiscussionDto, DiscussionViewItem>()
                .ForMember(x => x.EmployeeLockId, x => x.Ignore())
                .ForMember(x => x.EmployeeLockName, x => x.Ignore())
                .ForMember(x => x.TaskControlBitrix, x => x.Ignore())
                .ForMember(x => x.UseBitrixGroupFromExecutorDepartment, x => x.Ignore())
                .ForMember(x => x.ClientLogs, x => x.Ignore())
                .ForMember(x => x.FormatBody, x => x.Ignore());
            CreateMap<DiscussionParameter, DiscussionViewItem>(MemberList.Source)
                .ForSourceMember(x => x.DocumentId, x => x.DoNotValidate())
                .ForSourceMember(x => x.IsNew, x => x.DoNotValidate())
                .ForSourceMember(x => x.EntityId, x => x.DoNotValidate())
                .ForSourceMember(x => x.ShowAllTypes, x => x.DoNotValidate());
            CreateMap<DiscussionViewItem, DiscussionUpdateDto>();
            CreateMap<DiscussionViewItem, DiscussionCreateDto>();
            CreateMap<DiscussionEntityDocumentDto, DiscussionEntityDocumentViewItem>();
            CreateMap<DiscussionEntityDocumentViewItem, DiscussionEntityDocumentDto>();
            CreateMap<GuestProductReportDataDto, GuestProductReportData>()
                .ForMember(
                    x => x.Description,
                    y => y.MapFrom(x => string.IsNullOrEmpty(x.Description) ? x.Condition : $"{x.Condition}.{Environment.NewLine}{x.Description}"));
            CreateMap<TerminalPurchaseResult, TerminalDataDto>()
                .ForMember(x => x.TerminalMacAddress, y => y.Ignore());

            CreateMap<InvoiceBudgetDto, InvoiceBudgetViewItem>()
                .ForMember(x => x.DateFrom, x => x.MapFrom(z => z.DateFrom.ToDateTime(TimeOnly.MinValue)))
                .ForMember(x => x.DateTo, x => x.MapFrom(z => z.DateTo.ToDateTime(TimeOnly.MinValue)));
            CreateMap<InvoiceBudgetViewItem, InvoiceBudgetDto>()
                .ForMember(x => x.DateFrom, x => x.MapFrom(z => DateOnly.FromDateTime(z.DateFrom.Value)))
                .ForMember(x => x.DateTo, x => x.MapFrom(z => DateOnly.FromDateTime(z.DateTo.Value)));
            CreateMap<InvoiceCategoryBudgetDto, InvoiceCategoryBudgetViewItem>();
            CreateMap<InvoiceCategoryBudgetViewItem, InvoiceCategoryBudgetDto>();
            CreateMap<CreditOfferDto, CreditOfferViewItem>();
            CreateMap<CreditOfferViewItem, CreditOfferDto>();

            CreateMap<NpDocumentDto, ScanSheetEntityViewItem>()
                .ForMember(x => x.Id, y => y.MapFrom(z => z.EntityId))
                .ForMember(x => x.TtnId, y => y.MapFrom(z => z.Id))
                .ForMember(x => x.Selected, y => y.Ignore());
        }
    }
}