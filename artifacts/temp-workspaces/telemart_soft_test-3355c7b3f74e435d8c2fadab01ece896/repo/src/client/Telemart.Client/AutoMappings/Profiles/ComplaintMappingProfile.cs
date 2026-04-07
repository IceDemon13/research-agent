using AutoMapper;
using Telemart.Client.AutoMappings.ValueResolvers;
using Telemart.Client.Dictionaries;
using Telemart.Client.TransferObjects.Complaint;
using Telemart.Client.ViewModels.Complaint;

namespace Telemart.Client.AutoMappings.Profiles
{
    public class ComplaintMappingProfile : Profile
    {
        public ComplaintMappingProfile()
        {
            CreateMap<ComplaintDto, ComplaintViewItem>()
                .ForMember(x => x.Priority, y => y.MapFrom<DictionaryItemValueResolver<Priority>, int>(z => z.PriorityId))
                .ForMember(x => x.State, y => y.MapFrom<DictionaryItemValueResolver<ComplaintState>, int>(z => z.StateId));
            CreateMap<ComplaintParameter, ComplaintViewItem>(MemberList.Source)
                .ForSourceMember(x => x.IsNew, x => x.DoNotValidate());
            CreateMap<ComplaintViewItem, ComplaintSaveDto>()
                .ForMember(x => x.PriorityId, y => y.MapFrom(z => z.Priority.Id));
        }
    }
}