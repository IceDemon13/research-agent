using AutoMapper;
using Telemart.Client.AutoMappings.ValueResolvers;
using Telemart.Client.Common.MvvmEnhancements;
using Telemart.Client.Dictionaries;
using Telemart.Client.TransferObjects;
using Telemart.Client.ViewModels.Service.ServiceRequests;

namespace Telemart.Client.AutoMappings.Profiles
{
    public sealed class ServiceRequestMappingProfile : Profile
    {
        public ServiceRequestMappingProfile()
        {
            CreateMap<ServiceRequestDto, ServiceRequestViewItem>()
                .ForMember(x => x.RealCompletedOnFiscalRegistrar, x => x.MapFrom(y => !string.IsNullOrEmpty(y.FiscalId)))
                .ForMember(x => x.SelectedServiceProductId, x => x.Ignore())
                .ForMember(x => x.SelectedRefundId, x => x.Ignore())
                .ForMember(x => x.PurchasedSummary, x => x.Ignore())
                .ForMember(x => x.Subdivision, y => y.MapFrom<SubdivisionResolver, int>(z => z.SubdivisionId))
                .ForMember(x => x.Requirement, y => y.MapFrom<DictionaryItemValueResolver<ServiceRequestRequirement>, int>(z => z.Requirement))
                .ForMember(x => x.RequirementResolution, y => y.MapFrom<DictionaryItemValueResolver<ServiceRequestResolution>, int>(z => z.RequirementResolution ?? 0))
                .ForMember(x => x.State, y => y.MapFrom<DictionaryItemValueResolver<ServiceRequestState>, int>(z => z.StateId))
                .ForMember(x => x.EmployeeLockName, y => y.MapFrom(z => z.EmployeeLock.ShortName))
                .ForMember(x => x.CompletenessId, y => y.MapFrom(z => z.CompletenessId > 0 ? z.CompletenessId : (int?)null))
                .ForMember(x => x.CarryIn, y => y.MapFrom<CarryTypeResolver, int>(z => z.CarryInId ?? -1))
                .ForMember(x => x.CarryOut, y => y.MapFrom<CarryTypeResolver, int>(z => z.CarryOutId ?? -1))
                .ForMember(x => x.ReadyForRecomplectation, x => x.MapFrom(z => GetReadyForRecomplectation(z)))
                .ForMember(x => x.CustomerStateText, x => x.Condition(z => !string.IsNullOrWhiteSpace(z.CustomerStateText)))
                .ForMember(x => x.Group, y => y.MapFrom(z => z.Group == null ? (ComboBoxItem?)null : new ComboBoxItem(z.Group.GroupId, $"{z.Group.GroupId}{(z.Group.Finished ? " (Завершена)" : string.Empty)}", true, null)));

            CreateMap<ServiceRequestDiscussionDto, ServiceRequestDiscussionViewItem>();

            CreateMap<ServiceRequestViewItem, ServiceRequestSaveDto>()
                .ForMember(x => x.Documents, x => x.Ignore())
                .ForMember(x => x.TtnIn, y => y.MapFrom(z => z.TtnIn));
        }

        private static bool? GetReadyForRecomplectation(ServiceRequestDto dto)
        {
            if (dto.NewAssembledComputerActive is null || dto.StateId != ServiceRequestState.InProgress.Id)
            {
                return null;
            }

            return dto.NewAssembledComputerActive.Value;
        }
    }
}