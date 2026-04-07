using AutoMapper;
using Telemart.Client.AutoMappings.ValueResolvers;
using Telemart.Client.Dictionaries;
using Telemart.Client.TransferObjects.Bitrix;
using Telemart.Client.ViewModels.Bitrix;

namespace Telemart.Client.AutoMappings.Profiles
{
    public class BitrixMappingProfile : Profile
    {
        public BitrixMappingProfile()
        {
            CreateMap<BitrixTaskDto, CategorizedBitrixTaskViewItem>()
                .ForMember(x => x.Id, x => x.Ignore())
                .ForMember(x => x.Position, x => x.Ignore())
                .ForMember(x => x.CategoryIds, x => x.Ignore())
                .ForMember(x => x.Error, x => x.Ignore())
                .ForMember(x => x.Processed, x => x.Ignore())
                .ForMember(x => x.Priority, y => y.MapFrom<DictionaryItemValueResolver<Priority>, int>(z => z.PriorityId))
                .ForMember(x => x.OurPriority, y => y.MapFrom<DictionaryItemValueResolver<Priority>, int>(z => z.PriorityId));
            CreateMap<CategorizedBitrixTaskDto, CategorizedBitrixTaskViewItem>()
                .ForMember(x => x.Processed, x => x.Ignore())
                .ForMember(x => x.Error, x => x.Ignore())
                .ForMember(x => x.Priority, y => y.MapFrom<DictionaryItemValueResolver<Priority>, int>(z => z.PriorityId))
                .ForMember(x => x.OurPriority, y => y.MapFrom<DictionaryItemValueResolver<Priority>, int>(z => z.OurPriorityId));
            CreateMap<BitrixCategoryDto, BitrixCategoryViewItem>()
                .ForMember(x => x.Checked, x => x.Ignore());
        }
    }
}