using System.Collections.Generic;
using System.Linq;
using AutoMapper;
using Telemart.Client.AutoMappings.ValueResolvers;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.TransferObjects;
using Telemart.Client.ViewModels.AssembledComputerRule;

namespace Telemart.Client.AutoMappings.Profiles
{
   public class AssembledComputerRuleMappingProfile : Profile
    {
       public AssembledComputerRuleMappingProfile()
       {
           CreateMap<AssembledComputerRuleDto, AssembledComputerRuleViewItem>()
               .Ignore(x => x.ColorPrimary)
               .Ignore(x => x.ColorSecondary)
               .ForMember(x => x.Id, x => x.SetMappingOrder(0))
               .ForMember(x => x.EmployeeLockId, x => x.Ignore())
               .ForMember(x => x.EmployeeLockName, x => x.Ignore())
               .ForMember(x => x.Model, x => x.Ignore())
               .ForMember(x => x.Brand, x =>
               {
                   x.MapFrom(z => new BrandViewItem(z.Brand.Id, $"{z.Brand.ParentName}/{z.Brand.Name}", z.Brand.Name, z.Brand.PrefixRus, z.Brand.PrefixUkr, z.Brand.PrefixEn));
                   x.SetMappingOrder(1);
               });

           CreateMap<AssembledComputerRuleDto, AssembledComputerRulesViewItem>();
           CreateMap<AssembledComputerRuleCategoryDto, AssembledComputerRuleCategoryViewItem>()
                .ForMember(x => x.ProductTypes, y => y.MapFrom<DictionaryItemsToObservableCollectionValueResolver<ProductType>, List<int>>(z => z.ProductTypeIds.ToList()));
           CreateMap<AssembledComputerRuleProductDto, AssembledComputerRuleProductViewItem>();
           CreateMap<AssembledComputerRuleParameter, AssembledComputerRuleViewItem>(MemberList.Source)
               .ForSourceMember(x => x.IsNew, x => x.DoNotValidate())
               .ForSourceMember(x => x.ProductName, x => x.DoNotValidate());
           CreateMap<AssembledComputerRuleReserveProductDto, AssembledComputerRuleReserveProductViewItem>()
               .ForMember(x => x.WarehouseQuantityCalculatedForWarehouseId, x => x.MapFrom(z => z.WarehouseId))
               .ForMember(x => x.FactReserveQuantity, x => x.MapFrom(z => z.ReservedByAssembledComputerRuleQuantity));
       }
   }
}