using AutoMapper;
using Telemart.Client.TransferObjects.ProductCompatibility;
using Telemart.Client.ViewModels.ProductCompatibility;

namespace Telemart.Client.AutoMappings.Profiles
{
    public class ProductCompatibilityMappingProfile : Profile
    {
        public ProductCompatibilityMappingProfile()
        {
            CreateMap<ProductCompatibilityDto, ProductCompatibilityViewItem>()
                .ForMember(x => x.AllowEditNotificationImageId, x => x.Ignore());
            CreateMap<ProductCompatibilityViewItem, ProductCompatibilitySaveDto>()
                .ForSourceMember(x => x.AllowEditNotificationImageId, x => x.DoNotValidate())
                .ForMember(x => x.MasterFeatureId, y => y.MapFrom(z => z.MasterFeature.Value.Id))
                .ForMember(x => x.SlaveFeatureId, y => y.MapFrom(z => z.SlaveFeature.Value.Id));
            CreateMap<ProductCompatibilityParameter, ProductCompatibilityViewItem>(MemberList.Source)
                .ForMember(x => x.AllowEditNotificationImageId, x => x.Ignore())
                .ForSourceMember(x => x.IsNew, x => x.DoNotValidate());
        }
    }
}