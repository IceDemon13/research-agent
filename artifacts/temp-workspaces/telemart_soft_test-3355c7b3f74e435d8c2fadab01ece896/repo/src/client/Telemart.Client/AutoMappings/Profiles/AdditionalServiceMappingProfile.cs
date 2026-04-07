using AutoMapper;
using Telemart.Client.TransferObjects;
using Telemart.Client.ViewModels.AdditionalService;

namespace Telemart.Client.AutoMappings.Profiles
{
    public class AdditionalServiceMappingProfile : Profile
    {
        public AdditionalServiceMappingProfile()
        {
            CreateMap<AdditionalServiceGroupDto, AdditionalServiceGroupViewItem>()
                .ForMember(x => x.Groups, x => x.Ignore())
                .ForMember(x => x.Children, x => x.Ignore())
                .ForMember(x => x.ParentName, x => x.Ignore());

            CreateMap<AdditionalServiceGroupDto, AdditionalServiceGroupSimpleViewItem>()
                .ForMember(x => x.ParentName, x => x.Ignore());

            CreateMap<AdditionalServiceDto,  AdditionalServiceViewItem>();
            CreateMap<AdditionalServiceDto, AdditionalServicesViewItem>();

            CreateMap<AdditionalServiceGroupParameter, AdditionalServiceGroupSimpleViewItem>(MemberList.Source)
                .ForSourceMember(x => x.IsNew, x => x.DoNotValidate())
                .ForMember(x => x.MultiSelect, y => y.MapFrom(x => true));
            CreateMap<AdditionalServiceGroupParameter, AdditionalServiceGroupViewItem>(MemberList.Source)
                .ForSourceMember(x => x.ParentId, x => x.DoNotValidate())
                .ForSourceMember(x => x.ParentName, x => x.DoNotValidate())
                .ForSourceMember(x => x.IsNew, x => x.DoNotValidate())
                .ForMember(x => x.MultiSelect, y => y.MapFrom(x => true));

            CreateMap<AdditionalServiceParameter, AdditionalServiceViewItem>(MemberList.Source)
                .ForSourceMember(x => x.IsNew, x => x.DoNotValidate());

            CreateMap<AdditionalServiceSlaveCategoryDto, AdditionalServiceSlaveCategoryViewItem>();
            CreateMap<AdditionalServiceProvideRuleDto, AdditionalServiceProvideRuleItem>();
        }
    }
}