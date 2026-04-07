using AutoMapper;
using Telemart.Client.AutoMappings.ValueResolvers;
using Telemart.Client.Dictionaries;
using Telemart.Client.TransferObjects.Task;
using Telemart.Client.ViewModels.Tasks;

namespace Telemart.Client.AutoMappings.Profiles
{
    public sealed class TaskMappingProfile : Profile
    {
        public TaskMappingProfile()
        {
            CreateMap<TaskDto, TaskViewItem>()
                .ForMember(x => x.State, y => y.MapFrom<DictionaryItemValueResolver<TaskState>, int>(z => z.StateId));
        }
    }
}
