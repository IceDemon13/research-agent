using AutoMapper;
using Telemart.Client.AutoMappings.ValueResolvers;
using Telemart.Client.Common.Messages;
using Telemart.Client.Dictionaries;
using Telemart.Client.TransferObjects.Call;
using Telemart.Client.ViewModels.Store.Call;

namespace Telemart.Client.AutoMappings.Profiles
{
    public sealed class CallMappingProfile : Profile
    {
        public CallMappingProfile()
        {
            CreateMap<CallDto, CallViewItem>()
                .ForMember(x => x.ParrentId, x => x.Ignore())
                .ForMember(x => x.IsFolder, x => x.Ignore())
                .ForMember(x => x.IsCompleted, x => x.Ignore())
                .ForMember(x => x.Subdivision, y => y.MapFrom<SubdivisionResolver, int>(z => z.SubdivisionId))
                .ForMember(x => x.Priority, y => y.MapFrom<DictionaryItemValueResolver<Priority>, int>(z => z.PriorityId))
                .ForMember(x => x.CallState, y => y.MapFrom<DictionaryItemValueResolver<CallState>, int>(z => z.StateId))
                .ForMember(x => x.EmployeeLockName, y => y.MapFrom(z => z.EmployeeLock.ShortName));

            CreateMap<CallViewMessage, CallViewItem>(MemberList.Source)
                .ForSourceMember(x => x.IsNew, x => x.DoNotValidate());

            CreateMap<CallViewItem, CallSaveDto>()
                .ForMember(x => x.PriorityId, y => y.MapFrom(z => z.Priority.Id));
        }
    }
}