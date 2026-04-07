using System.Collections.Generic;
using System.Linq;
using AutoMapper;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.Helpers;
using Telemart.Client.TransferObjects;
using Telemart.Client.ViewModels.AdditionalServiceProduct;

namespace Telemart.Client.AutoMappings.Profiles
{
    public class AdditionalServiceProductMappingProfile : Profile
    {
        public AdditionalServiceProductMappingProfile()
        {
            CreateMap<AdditionalServiceProductDto, AdditionalServiceProductsViewItem>()
                .ForMember(x => x.Name, y => y.MapFrom(z => z.ProductName))
                .ForMember(x => x.NameUkr, y => y.MapFrom(z => z.ProductNameUa))
                .ForMember(x => x.NameEn, y => y.MapFrom(z => z.ProductNameEn))
                .ForMember(x => x.ProductName, y => y.Ignore())
                .ForMember(x => x.OrderComment, x => x.MapFrom(z => OrderCommentHelper.GetJoinedComment(z.CustomerComment, z.EmployeeComment, z.SystemComment)))
                .ForMember(x => x.AdditionalServiceName, y => y.MapFrom(z => z.GetLocalName(LocalizableNameType.Ukr)));
            CreateMap<AdditionalServiceProductDto, AdditionalServiceProductViewItem>()
                .ForMember(x => x.ProductWithConsumables, y => y.MapFrom(z => GetProductWithConsumables(z)))
                .ForMember(x => x.AdditionalServiceName, y => y.MapFrom(z => z.GetLocalName(LocalizableNameType.Ukr)));
        }

        private static IEnumerable<AdditionalServiceProductSnViewItem> GetProductWithConsumables(AdditionalServiceProductDto source)
        {
            yield return new AdditionalServiceProductSnViewItem()
            {
                ProductId = source.ProductId,
                ProductTypeId = source.ProductTypeId,
                Name = source.ProductName,
                NameUkr = source.ProductNameUa,
                NameEn = source.ProductNameEn,
                SerialNumber = source.SerialNumber,
                Scanned = source.Scanned,
                KeepSerial = source.KeepSerial,
                GroupString = "Товар",
                GuestProduct = source.GuestProduct
            };

            if (source.ConsumableProducts?.Any() == true)
            {
                foreach (AdditionalServiceProductConsumableDto consumableProduct in source.ConsumableProducts)
                {
                    yield return new AdditionalServiceProductSnViewItem()
                    {
                        ConsumableId = consumableProduct.Id,
                        Scanned = consumableProduct.Scanned,
                        SerialNumber = consumableProduct.SerialNumber,
                        Name = consumableProduct.ProductName,
                        NameUkr = consumableProduct.ProductNameUa,
                        NameEn = consumableProduct.ProductNameEn,
                        ProductId = consumableProduct.ProductId,
                        ProductTypeId = consumableProduct.ProductTypeId,
                        KeepSerial = consumableProduct.KeepSerial,
                        GroupString = "Товары для оказания услуги",
                        GuestProduct = consumableProduct.GuestProduct
                    };
                }
            }
        }
    }
}