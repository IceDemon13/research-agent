using System.Linq;
using AutoMapper;
using Telemart.Client.AutoMappings.ValueResolvers;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.TransferObjects;
using Telemart.Client.ViewModels.Common.Product;
using Telemart.Client.ViewModels.Directories.ProductsCatalog;
using Telemart.Client.ViewModels.Nomenclature;
using Telemart.Client.ViewModels.Tools.Tags;

namespace Telemart.Client.AutoMappings.Profiles
{
    public sealed class ProductMappingProfile : Profile
    {
        public ProductMappingProfile()
        {
            CreateMap<ProductCardDto, ProductCardViewItem>()
                .ConstructUsing(x => ProductCardViewItem.Create())
                .ForMember(x => x.CategoryId, y => y.MapFrom(z => z.CategoryId))
                .ForMember(x => x.Category, y => y.MapFrom(z => z.Category))
                .ForMember(x => x.CurrencyId, y => y.MapFrom(x => Currency.GetByName(x.Currency).Id));
            CreateMap<ProductCardViewItem, ProductCardSaveDto>()
                .ForMember(x => x.ActiveBarcodeIds, y => y.MapFrom(z => z.Barcodes.Where(x => x.Active).Select(x => x.Id)))
                .ForMember(x => x.FiscalRegistrarBarcodeIds, y => y.MapFrom(z => z.Barcodes.Where(x => x.FiscalRegistrar).Select(x => x.Id)));

            CreateMap<ProductBarcodeDto, ProductBarcodeViewItem>()
                .ConstructUsing(x => ProductBarcodeViewItem.Create());

            CreateMap<ProductSnLengthDto, ProductSnLengthViewItem>();
            CreateMap<ProductSnLengthViewItem, ProductSnLengthDto>();

            CreateMap<ProductCatalogDto, ProductCatalogViewItem>()
                .ForMember(x => x.ColorPrimary, x => x.Ignore())
                .ForMember(x => x.ColorSecondary, x => x.Ignore())
                .ForMember(x => x.ActiveExpected, y => y.MapFrom(x => x.Active.Equals(0.5) ? null : (bool?)x.Active.Equals(1)));
            CreateMap<ProductCatalogViewItem, ProductCatalogViewItemWrapper>()
                .ForMember(x => x.Category, x => x.Ignore())
                .ForMember(x => x.IsEdited, x => x.Ignore())
                .ForMember(x => x.Guid, x => x.Ignore())
                .ConstructUsing(x => new ProductCatalogViewItemWrapper(x));
            CreateMap<ProductCatalogImportItem, ProductCatalogViewItemWrapper>()
                .ForMember(x => x.CategoryId, y => y.Ignore())
                .ForMember(x => x.Name, y => y.Ignore())
                .ForMember(x => x.NameUkr, y => y.Ignore())
                .ForMember(x => x.NameEn, y => y.Ignore())
                .ForMember(x => x.Category, y => y.Ignore())
                .ForMember(x => x.IsEdited, y => y.Ignore())
                .ForMember(x => x.Guid, y => y.Ignore())
                .ForMember(x => x.BasedOn, y => y.Ignore())
                .ForMember(x => x.ColorPrimaryId, y => y.Ignore())
                .ForMember(x => x.ColorSecondaryId, y => y.Ignore())
                .ForMember(x => x.AssembledComputerRuleBaseProductId, y => y.Ignore())
                .ForMember(x => x.CreatedOn, y => y.Ignore())
                .ForMember(x => x.IsNo, y => y.Ignore())
                .ForMember(x => x.Active, y => y.Ignore())
                .ForMember(x => x.ActiveExpected, y => y.Ignore())
                .ForMember(x => x.ActivatedOn, y => y.Ignore())
                .Ignore(x => x.ColorPrimary)
                .Ignore(x => x.ColorSecondary)
                .ForMember(x => x.GroupFeatureId, y => y.MapFrom(z => z.GroupFeatureId == 0 ? null : z.GroupFeatureId));
            CreateMap<ProductCatalogViewItemWrapper, ProductCatalogSaveDto>()
                .ForMember(x => x.ActiveExpected, y => y.MapFrom(x => x.ActiveExpected.HasValue ? (x.ActiveExpected.Value ? 1 : 0) : 0.5))
                .ForMember(x => x.Color, y => y.MapFrom(x => x.Color ?? string.Empty))
                .ForMember(x => x.YandexId, y => y.MapFrom(x => x.YandexId ?? string.Empty))
                .ForMember(x => x.Keywords, y => y.MapFrom(x => x.Keywords ?? string.Empty))
                .ForMember(x => x.PrefixRus, y => y.MapFrom(x => x.PrefixRus ?? string.Empty))
                .ForMember(x => x.PrefixUkr, y => y.MapFrom(x => x.PrefixUkr ?? string.Empty))
                .ForMember(x => x.PrefixEn, y => y.MapFrom(x => x.PrefixEn ?? string.Empty))
                .ForMember(x => x.PartNumber, y => y.MapFrom(x => string.IsNullOrEmpty(x.PartNumber) ? null : x.PartNumber));

            CreateMap<ProductColorDto, ProductColorViewItem>()
                .ConstructUsing(x => ProductColorViewItem.Create());

            CreateMap<ProductTagDto, ProductTagViewItem>()
                .ForMember(x => x.Id, y => y.MapFrom(x => x.Id))
                .ForMember(x => x.Category, y => y.MapFrom(x => x.Category))
                .ForMember(x => x.Name, y => y.MapFrom(x => x.Name))
                .ForMember(x => x.Price, y => y.MapFrom(x => x.Price))
                .ForMember(x => x.TagFormat, y => y.MapFrom<DictionaryItemValueResolver<TagFormat>, int>(z => z.TagFormatId))
                .ForMember(x => x.Color, y => y.MapFrom<DictionaryItemValueResolver<TagColor>, int>(z => z.ColorId == 0 ? 1 : z.ColorId));

            CreateMap<ProductDto, NomenclatureViewItem>()
                .ForMember(x => x.AssemblyIncluded, x => x.Ignore())
                .ForMember(x => x.AdditionalService, x => x.Ignore())
                .ForMember(x => x.Id, y => y.MapFrom(z => z.Id))
                .ForMember(x => x.Name, y => y.MapFrom(z => z.Name))
                .ForMember(x => x.NameFullRu, y => y.MapFrom(z => z.NameFullRu))
                .ForMember(x => x.NameFullUa, y => y.MapFrom(z => z.NameFullUa))
                .ForMember(x => x.Category, y => y.MapFrom(z => z.ParentCategoryName))
                .ForMember(x => x.CurrencyId, y => y.MapFrom(z => z.CurrencyId))
                .ForMember(x => x.Price, y => y.MapFrom(z => z.Price))
                .ForMember(x => x.Weight, y => y.MapFrom(z => z.Weight ?? z.WeightEstimated))
                .ForMember(x => x.ProductPn, y => y.MapFrom(z => z.Pn))
                .ForMember(x => x.TaxRateId, y => y.MapFrom(z => z.TaxRateId))
                .ForMember(x => x.Quantity, y => y.MapFrom(z => z.AssemblyQuantity))
                .ForMember(x => x.WarehouseQuantity, y => y.MapFrom(z => z.Warehouses.Sum(w => w.QuantityFree)))
                .ForMember(x => x.WarehousePrice, y => y.MapFrom(z => z.Warehouses.Sum(w => w.QuantityFree) > 0
                    ? z.Warehouses.Sum(x => x.Price * x.QuantityFree) / z.Warehouses.Sum(w => w.QuantityFree)
                    : 0))
                .ForMember(x => x.Gifts, y => y.MapFrom(z => z.Gifts))
                .ForMember(x => x.AdditionalServiceGroups, y => y.MapFrom(z => z.AdditionalServiceGroups));

            CreateMap<ProductAttributesDto, ProductDimensionsViewItem>()
                .ForMember(x => x.Id, y => y.MapFrom(z => z.ProductId));
            CreateMap<ProductDimensionsDto, ProductDimensionsViewItem>();
        }
    }
}