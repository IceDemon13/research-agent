using System.Linq;
using AutoMapper;
using Telemart.Client.AutoMappings.ValueResolvers;
using Telemart.Client.Business;
using Telemart.Client.Core.Extensions;
using Telemart.Client.Dictionaries;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.ReturnInvoice;
using Telemart.Client.ViewModels.Store;
using Telemart.Client.ViewModels.Store.Invoice;
using Telemart.Client.ViewModels.Store.Purchase;
using Telemart.Client.ViewModels.Store.ReturnInvoice;

namespace Telemart.Client.AutoMappings.Profiles
{
    public sealed class InvoiceMappingProfile : Profile
    {
        public InvoiceMappingProfile()
        {
            CreateMap<InvoiceDto, MassInvoiceAcceptViewItem>()
                   .ForMember(x => x.InvoiceId, y => y.MapFrom(z => z.Id));
            CreateMap<InvoiceDto, InvoiceViewItem>()
                .ConstructUsing(x => InvoiceViewItem.Create())
                .ForMember(x => x.AnalyzeWasDone, x => x.Ignore())
                .ForMember(x => x.State, y => y.MapFrom<DictionaryItemValueResolver<InvoiceState>, int>(z => z.StateId))
                .ForMember(x => x.TotalPrice, y => y.MapFrom<InvoiceTotalPriceResolver>())
                .ForMember(x => x.PriceImage, y => y.MapFrom<InvoicePriceImageResolver>())
                .ForMember(x => x.CarryType, y => y.MapFrom<CarryTypeResolver, int>(x => x.CarryId))
                .ForMember(x => x.InvoiceTtns, y => y.MapFrom<InvoiceTtnViewItemResolver>())
                .ForMember(x => x.CurrencyRateStr, y => y.MapFrom(z => GetCurrencyRateStr(z)));
            CreateMap<InvoiceProductDto, InvoiceProductViewItem>()
                .ForMember(x => x.PriceConverter, x => x.Ignore())
                .ForMember(x => x.ExtraCharge, x => x.Ignore())
                .ForMember(x => x.PriceTelemartUah, x => x.Ignore())
                .ForMember(x => x.OriginalCurrency, x => x.Ignore())
                .ForMember(x => x.ImageToolTip, x => x.Ignore())
                .ForMember(x => x.ErrorImage, x => x.Ignore())
                .ForMember(x => x.Barcodes, x => x.Ignore())
                .ForMember(x => x.IsEditableForCurrentUser, x => x.Ignore())
                .ForMember(x => x.CanEditCurrency, x => x.Ignore())
                .ForMember(x => x.PriceRedColor, x => x.Ignore())
                .ForMember(x => x.PriceGreenColor, x => x.Ignore())
                .ForMember(x => x.Currency, y => y.MapFrom<CurrencyTypeResolver, int>(x => x.CurrencyId))
                .ForMember(x => x.ProductId, y => y.MapFrom(z => z.ProductId))
                .ForMember(x => x.ProductName, y => y.MapFrom(z => z.ProductName))
                .ForMember(x => x.ProductPrefix, y => y.MapFrom(z => z.ProductPrefix))
                .ForMember(x => x.FullName, y => y.MapFrom(z => z.ProductName.GetStringWithPrefix(z.ProductPrefix)))
                .ForMember(x => x.FullNameUa, y => y.MapFrom(z => z.ProductNameUa.GetStringWithPrefix(z.ProductPrefixUa)))
                .ForMember(x => x.FullNameEn, y => y.MapFrom(z => z.ProductNameEn.GetStringWithPrefix(z.ProductPrefixEn)));

            CreateMap<InvoiceProductDto, InvoiceDelayProductViewItem>()
                .ForMember(x => x.AlternativeId, x => x.Ignore())
                .ForMember(x => x.RemoveSource, x => x.Ignore())
                .ForMember(x => x.ProductId, y => y.MapFrom(z => z.ProductId))
                .ForMember(x => x.FullName, y => y.MapFrom(z => z.ProductName.GetStringWithPrefix(z.ProductPrefix)))
                .ForMember(x => x.FullNameUa, y => y.MapFrom(z => z.ProductNameUa.GetStringWithPrefix(z.ProductPrefixUa)))
                .ForMember(x => x.FullNameEn, y => y.MapFrom(z => z.ProductNameEn.GetStringWithPrefix(z.ProductPrefixEn)));

            CreateMap<InvoiceProductViewItem, InvoiceProductSaveDto>()
                .ForMember(x => x.CurrencyId, y => y.MapFrom(r => r.Currency.Id));

            CreateMap<PurchaseInvoiceSourceDto, SetInvoiceSourceViewItem>()
                .ForMember(x => x.OrderQuantity, x => x.Ignore())
                .ForMember(x => x.IsNewRow, x => x.Ignore())
                .ForMember(x => x.OrderDeliveryDateTime, x => x.Ignore())
                .ForMember(x => x.OrderState, x => x.Ignore())
                .ForMember(x => x.CarryType, y => y.MapFrom<CarryTypeResolver, int>(x => x.CarryId))
                .ForMember(
                    x => x.State,
                    y => y.MapFrom<DictionaryItemValueResolver<InvoiceState>, int>(z => z.InvoiceStateId ?? 0));

            CreateMap<InvoiceTemplateDto, InvoiceTemplateViewItem>()
                .ForMember(x => x.State, y => y.MapFrom<DictionaryItemValueResolver<InvoiceState>, int>(z => z.StateId ?? 0))
                .ForMember(x => x.CarryType, y => y.MapFrom<CarryTypeResolver, int>(x => x.CarryId));

            CreateMap<InvoiceProductViewItem, InvoiceProductViewItem>();
            CreateMap<InvoiceViewItem, InvoiceViewItem>();

            CreateMap<InvoiceProductDto, ReturnInvoiceProductViewItem>()
                .ForMember(x => x.CategoryId, x => x.Ignore())
                .ForMember(x => x.CategoryName, x => x.Ignore())
                .ForMember(x => x.AcceptQuantity, x => x.Ignore())
                .ForMember(x => x.OutQuantity, x => x.Ignore())
                .ForMember(x => x.SerialsQuantity, x => x.Ignore())
                .ForMember(x => x.FullName, x => x.Ignore())
                .ForMember(x => x.CategoryType, x => x.Ignore())
                .ForMember(x => x.KeepSerial, x => x.Ignore())
                .ForMember(x => x.UsdCurrency, x => x.Ignore())
                .ForMember(x => x.StockQuantity, x => x.Ignore())
                .ForMember(x => x.EmployeeLockId, x => x.Ignore())
                .ForMember(x => x.EmployeeLockName, x => x.Ignore())
                .ForMember(x => x.StockQuantity, x => x.Ignore())
                .ForMember(x => x.FullNameUkr, x => x.Ignore())
                .ForMember(x => x.Currency, y => y.MapFrom<CurrencyTypeResolver, int>(x => x.CurrencyId))
                .ForMember(x => x.MaxQuantityDefaultFromInvoice, y => y.MapFrom(z => z.QuantityReal - z.QuantityReturned));

            CreateMap<InvoiceCurrencyRateDto, InvoiceCurrencyRateViewItem>()
                .ForMember(x => x.Name, x => x.Ignore());
            CreateMap<InvoiceCurrencyRateViewItem, InvoiceCurrencyRateDto>()
                .ForMember(x => x.InvoiceId, x => x.Ignore())
                .ForMember(x => x.ModifiedBy, x => x.Ignore())
                .ForMember(x => x.ModifiedOn, x => x.Ignore())
                .ForMember(x => x.CreatedOn, x => x.Ignore())
                .ForMember(x => x.CreatedBy, x => x.Ignore());

            CreateMap<ReturnInvoiceDto, ReturnInvoiceViewItem>()
                .ForMember(x => x.DocumentsCount, x => x.Ignore());
            CreateMap<ReturnInvoiceProductViewItem, ReturnInvoiceProductDto>();
            CreateMap<ReturnInvoiceProductDto, ReturnInvoiceProductViewItem>()
                .ConstructUsing(x => new ReturnInvoiceProductViewItem(x.Price))
                .ForMember(x => x.Currency, x => x.Ignore())
                .ForMember(x => x.SerialsQuantity, x => x.Ignore())
                .ForMember(x => x.StockQuantity, x => x.Ignore())
                .ForMember(x => x.EmployeeLockId, x => x.Ignore())
                .ForMember(x => x.EmployeeLockName, x => x.Ignore())
                .ForMember(x => x.CategoryType, x => x.MapFrom<DictionaryItemValueResolver<CategoryType>, int>(y => y.CategoryTypeId));

            CreateMap<InvoiceAdditionalCostDto, InvoiceAdditionalCostViewItem>()
                .ForMember(x => x.EmployeeLockId, x => x.Ignore())
                .ForMember(x => x.EmployeeLockName, x => x.Ignore());
            CreateMap<InvoiceAdditionalCostProductDto, InvoiceAdditionalCostProductViewItem>()
                .ForMember(x => x.Include, x => x.Ignore());
            CreateMap<AdditionalCostParameter, InvoiceAdditionalCostViewItem>(MemberList.Source)
                .ForSourceMember(x => x.IsNew, x => x.DoNotValidate());
        }

        private static string GetCurrencyRateStr(InvoiceDto dto)
        {
            string[] rates = dto.CurrencyRates?.Select(x => CurrencyFormatingRules.ToStr(x.ConversionRate, x.FromCurrencyId)).ToArray();

            return rates is null ? null : string.Join(", ", rates);
        }
    }
}