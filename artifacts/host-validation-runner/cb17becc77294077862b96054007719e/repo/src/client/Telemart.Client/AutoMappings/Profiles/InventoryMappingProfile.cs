using System;
using AutoMapper;
using Telemart.Client.TransferObjects;
using Telemart.Client.ViewModels.Warehouse.Inventory;

namespace Telemart.Client.AutoMappings.Profiles
{
    public class InventoryMappingProfile : Profile
    {
        public InventoryMappingProfile()
        {
            CreateMap<InventoryDto, InventoryViewItem>()
                .ForMember(x => x.Warehouse, x => x.Ignore())
                .ForMember(x => x.CreatedByEmployeeName, x => x.Ignore())
                .ForMember(x => x.Comment, y => y.MapFrom(z => z.Comment.Replace(Environment.NewLine, " ").Replace("\n", " ")))
                .ConstructUsing(x => InventoryViewItem.Create());
            CreateMap<InventoryProductDto, InventoryProductViewItem>()
                .ForMember(x => x.Quantity, y => y.MapFrom(x => x.Quantity))
                .ForMember(x => x.QuantityReal, y => y.MapFrom(x => x.QuantityReal))
                .ForMember(x => x.CurrentInventoryQuantity, y => y.MapFrom(x => x.Quantity))
                .ForMember(x => x.CurrentInventoryQuantityReal, y => y.MapFrom(x => x.QuantityReal))
                .ForMember(x => x.AssemblyServiceId, x => x.Ignore())
                .ForMember(x => x.FullName, x => x.Ignore())
                .ForMember(x => x.ParentCategoryName, x => x.Ignore())
                .ForMember(x => x.ParentCategoryId, y => y.Ignore())
                .ConstructUsing(x => InventoryProductViewItem.Create());
        }
    }
}