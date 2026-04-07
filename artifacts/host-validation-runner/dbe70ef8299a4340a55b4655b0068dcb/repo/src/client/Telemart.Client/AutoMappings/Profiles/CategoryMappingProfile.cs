using AutoMapper;
using Telemart.Client.TransferObjects;
using Telemart.Client.ViewModels.Directories.Category;

namespace Telemart.Client.AutoMappings.Profiles
{
    public class CategoryMappingProfile : Profile
    {
        public CategoryMappingProfile()
        {
            CreateMap<CategoryDto, CategoryViewItem>()
                .ForMember(x => x.NameFullUkr, x => x.Ignore())
                .ForMember(x => x.Selected, x => x.Ignore())
                .ForMember(x => x.Enabled, x => x.Ignore())
                .ConstructUsing(x => CategoryViewItem.Create());

            CreateMap<CategoryFullDto, CategoryViewItem>()
                .ForMember(x => x.Selected, x => x.Ignore())
                .ForMember(x => x.Enabled, y => y.Ignore())
                .ConstructUsing(x => CategoryViewItem.Create());

            CreateMap<CategoryFullDto, CategoryProductCatalogViewItem>()
                .ForMember(x => x.PartNumber, x => x.Ignore())
                .ForMember(x => x.Keywords, x => x.Ignore())
                .ConstructUsing(x => CategoryProductCatalogViewItem.Create());
        }
    }
}