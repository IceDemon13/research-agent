using AutoMapper;
using Telemart.Client.AutoMappings.ValueResolvers;
using Telemart.Client.Dictionaries;
using Telemart.Client.TransferObjects.ServiceProduct;
using Telemart.Client.ViewModels.Service.ServiceProducts;

namespace Telemart.Client.AutoMappings.Profiles
{
    public class ServiceProductMappingProfile : Profile
    {
        public ServiceProductMappingProfile()
        {
            CreateMap<ServiceProductDto, ServiceProductViewItem>()
                .ForMember(x => x.State, y => y.MapFrom<DictionaryItemValueResolver<ServiceProductState>, int>(z => z.StateId));

            CreateMap<ServiceProductViewItem, ServiceProductSaveDto>();
        }
    }
}