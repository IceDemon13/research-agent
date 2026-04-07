using AutoMapper;
using Telemart.Client.AutoMappings.ValueResolvers;
using Telemart.Client.Dictionaries;
using Telemart.Client.TransferObjects.Comment;
using Telemart.Client.ViewModels.Comment;

namespace Telemart.Client.AutoMappings.Profiles
{
    public class CommentMappingProfile : Profile
    {
        public CommentMappingProfile()
        {
            CreateMap<CommentSimpleDto, CommentViewItem>()
                .ForMember(x => x.Foto, x => x.Ignore())
                .ForMember(x => x.IsStateChanging, x => x.Ignore())
                .ForMember(x => x.Children, x => x.Ignore())
                .ForMember(x => x.State, y => y.MapFrom<DictionaryItemValueResolver<CommentState>, int>(z => z.StateId))
                .ForMember(x => x.Type, y => y.MapFrom<DictionaryItemValueResolver<CommentType>, int>(z => z.TypeId))
                .ForMember(x => x.Avatar, y => y.MapFrom<DictionaryItemValueResolver<AvatarType>, int>(z => z.AvatarId ?? 0));

            CreateMap<CommentFullDto, CommentViewItem>()
                .ForMember(x => x.IsStateChanging, x => x.Ignore())
                .ForMember(x => x.State, y => y.MapFrom<DictionaryItemValueResolver<CommentState>, int>(z => z.StateId))
                .ForMember(x => x.Type, y => y.MapFrom<DictionaryItemValueResolver<CommentType>, int>(z => z.TypeId))
                .ForMember(x => x.Avatar, y => y.MapFrom<DictionaryItemValueResolver<AvatarType>, int>(z => z.AvatarId ?? 0));
        }
    }
}