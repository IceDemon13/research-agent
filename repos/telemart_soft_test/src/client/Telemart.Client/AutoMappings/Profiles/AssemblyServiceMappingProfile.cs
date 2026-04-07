using AutoMapper;
using Telemart.Client.AutoMappings.ValueResolvers;
using Telemart.Client.Dictionaries;
using Telemart.Client.Helpers;
using Telemart.Client.TransferObjects.AssemblyService;
using Telemart.Client.ViewModels.AssemblyService;

namespace Telemart.Client.AutoMappings.Profiles
{
    public class AssemblyServiceMappingProfile : Profile
    {
        public AssemblyServiceMappingProfile()
        {
            CreateMap<AssemblyServiceDto, AssemblyServiceViewItem>();
            CreateMap<AssemblyServiceDto, AssemblyServicesViewItem>()
                .ForMember(x => x.OrderComment, x => x.MapFrom(z => OrderCommentHelper.GetJoinedComment(z.CustomerComment, z.EmployeeComment, z.SystemComment)))
                .ForMember(x => x.Subdivision, y => y.MapFrom<SubdivisionResolver, int>(z => z.SubdivisionId ?? 0))
                .ForMember(x => x.ProductsCount, x => x.MapFrom(z => z.Products.Count))
                .ForMember(x => x.KindOfJob, y => y.Ignore());
            CreateMap<AssemblyServiceProductDto, AssemblyServiceProductViewItem>()
                .ForMember(x => x.KeepSerialOverridden, x => x.Ignore())
                .ForMember(x => x.CategoryType, x => x.MapFrom<DictionaryItemValueResolver<CategoryType>, int>(y => y.CategoryTypeId));
            CreateMap<AssemblyServiceProductViewItem, AssemblyServiceProductDto>()
                .ForMember(x => x.OrderFolderId, x => x.Ignore())
                .ForMember(x => x.OrderId, x => x.Ignore());
        }
    }
}