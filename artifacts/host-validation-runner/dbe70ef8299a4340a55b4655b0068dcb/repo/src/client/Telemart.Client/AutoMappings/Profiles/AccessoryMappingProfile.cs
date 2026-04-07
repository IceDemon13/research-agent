using AutoMapper;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.Accessory;
using Telemart.Client.ViewModels.Accessory;

namespace Telemart.Client.AutoMappings.Profiles
{
    public class AccessoryMappingProfile : Profile
    {
        public AccessoryMappingProfile()
        {
            CreateMap<AccessoryDto, AccessoryViewItem>()
                .ForMember(x => x.DisplayCategories, x => x.Ignore());
            CreateMap<AccessoryCategoryDto, AccessoryCategoryViewItem>();
            CreateMap<AccessoryCategoryFeatureDto, AccessoryCategoryFeatureViewItem>()
                .ForMember(x => x.Id, x => x.Ignore());
            CreateMap<AccessoryFeatureDto, AccessoryFeatureViewItem>();
            CreateMap<AccessoryParameter, AccessoryViewItem>(MemberList.Source)
                .ForSourceMember(x => x.Copy, x => x.DoNotValidate())
                .ForSourceMember(x => x.IsNew, x => x.DoNotValidate());
        }
    }
}