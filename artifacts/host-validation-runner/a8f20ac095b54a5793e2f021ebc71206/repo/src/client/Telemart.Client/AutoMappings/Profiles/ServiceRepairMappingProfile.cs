using AutoMapper;
using Telemart.Client.AutoMappings.ValueResolvers;
using Telemart.Client.Dictionaries;
using Telemart.Client.TransferObjects;
using Telemart.Client.ViewModels.Service.ServiceRepairs;

namespace Telemart.Client.AutoMappings.Profiles
{
    public sealed class ServiceRepairMappingProfile : Profile
    {
        public ServiceRepairMappingProfile()
        {
            CreateMap<ServiceRepairDto, ServiceRepairViewItem>()
                .ForMember(x => x.State, y => y.MapFrom<DictionaryItemValueResolver<ServiceRepairState>, int>(z => z.StateId))
                .ForMember(x => x.ServiceRequestSubdivision, y => y.MapFrom<DictionaryItemValueResolver<Subdivision>, int>(z => z.ServiceRequestSubdivisionId));
        }
    }
}