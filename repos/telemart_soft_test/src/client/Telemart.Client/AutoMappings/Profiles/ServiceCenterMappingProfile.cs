using AutoMapper;
using Telemart.Client.AutoMappings.ValueResolvers;
using Telemart.Client.Common.Messages;
using Telemart.Client.Dictionaries;
using Telemart.Client.TransferObjects;
using Telemart.Client.ViewModels.Service.ServiceCenters;

namespace Telemart.Client.AutoMappings.Profiles
{
    public sealed class ServiceCenterMappingProfile : Profile
    {
        public ServiceCenterMappingProfile()
        {
            CreateMap<ServiceCenterDto, ServiceCenterViewItem>()
                .ForMember(x => x.Employee, x => x.Ignore())
                .ForMember(x => x.Type, y => y.MapFrom<DictionaryItemValueResolver<ServiceCenterType>, int>(z => z.TypeId))
                .ForMember(x => x.RepairConfirmType, y => y.MapFrom<DictionaryItemValueResolver<ServiceCenterRepairConfirmType>, int>(z => z.RepairConfirmTypeId))
                .ForMember(x => x.EmployeeLockName, y => y.MapFrom(z => z.EmployeeLock.ShortName));
            CreateMap<ServiceCenterViewMessage, ServiceCenterViewItem>(MemberList.Source)
                .ForSourceMember(x => x.IsNew, x => x.DoNotValidate());
            CreateMap<ServiceCenterViewItem, ServiceCenterSaveDto>()
                .ForMember(x => x.RepairConfirmTypeId, y => y.MapFrom(z => z.RepairConfirmType.Id));
        }
    }
}