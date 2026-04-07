using AutoMapper;
using Telemart.Client.TransferObjects.PromoCode;
using Telemart.Client.ViewModels.PromoCode;

namespace Telemart.Client.AutoMappings.Profiles
{
    public class PromoCodeMappingProfile : Profile
    {
        public PromoCodeMappingProfile()
        {
            CreateMap<PromoCodeDto, PromoCodeViewItem>();

            CreateMap<PromoCodeProductDto, PromoCodeProductViewItem>();
            CreateMap<PromoCodeProductViewItem, PromoCodeProductDto>();

            CreateMap<PromoCodeFullDto, PromoCodeFullViewItem>();
            CreateMap<PromoCodeParameter, PromoCodeFullViewItem>(MemberList.Source)
                .ForSourceMember(x => x.IsNew, x => x.DoNotValidate());

            CreateMap<PromoCodeFullViewItem, PromoCodeSaveDto>();

            CreateMap<PromoCodeBundleProductViewItem, PromoCodeBundleProductDto>();
            CreateMap<PromoCodeBundleProductDto, PromoCodeBundleProductViewItem>();

            CreateMap<PromoCodeBundleCategoryViewItem, PromoCodeBundleCategoryDto>();
            CreateMap<PromoCodeBundleCategoryDto, PromoCodeBundleCategoryViewItem>();
        }
    }
}