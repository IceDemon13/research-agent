using System.Linq;
using AutoMapper;
using Telemart.Client.AutoMappings.ValueResolvers;
using Telemart.Client.Dictionaries;
using Telemart.Client.TransferObjects.Backlog;
using Telemart.Client.ViewModels.Backlog;

namespace Telemart.Client.AutoMappings.Profiles
{
    public class BacklogMappingProfile : Profile
    {
        public BacklogMappingProfile()
        {
            CreateMap<BacklogTaskDto, BacklogTaskViewItem>()
                .ForMember(x => x.IsFavorite, x => x.Ignore())
                .ForMember(x => x.Employee, x => x.Ignore())
                .ForMember(x => x.Priority, y => y.MapFrom<DictionaryItemValueResolver<Priority>, int>(z => z.PriorityId))
                .ForMember(x => x.State, y => y.MapFrom<DictionaryItemValueResolver<BacklogTaskState>, int>(z => z.StateId))
                .ForMember(x => x.Resolution, y => y.MapFrom<DictionaryItemValueResolver<BacklogTaskResolution>, int>(z => z.ResolutionId ?? 0))
                .ForMember(x => x.CategoryIds, y => y.MapFrom(z => z.CategoryIds.OrderBy(x => x).ToArray()))
                .ForMember(x => x.EmployeeLockName, y => y.MapFrom(z => z.EmployeeLock.Name));

            CreateMap<BacklogTaskViewItem, BacklogTaskSaveDto>()
                .ForMember(x => x.StateId, y => y.MapFrom(z => z.State.Id))
                .ForMember(x => x.PriorityId, y => y.MapFrom(z => z.Priority.Id))
                .ForMember(x => x.ResolutionId, y => y.MapFrom(z => z.Resolution.Id));

            CreateMap<CreateBacklogTaskViewModel, BacklogTaskCreateDto>()
                .ForMember(x => x.BitrixId, y => y.MapFrom(z => z.BitrixId))
                .ForMember(x => x.Name, y => y.MapFrom(z => z.Name))
                .ForMember(x => x.Formulation, y => y.MapFrom(z => z.Formulation))
                .ForMember(x => x.Justification, y => y.MapFrom(z => z.Justification))
                .ForMember(x => x.Solution, y => y.MapFrom(z => z.Solution))
                .ForMember(x => x.CategoryIds, y => y.MapFrom(z => z.SelectedCategoryIds));

            CreateMap<BacklogCategoryDto, BacklogCategoryViewItem>()
                .ForMember(x => x.Marked, x => x.Ignore())
                .ForMember(x => x.Checked, y => y.MapFrom(z => false));
        }
    }
}