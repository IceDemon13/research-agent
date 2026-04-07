using AutoMapper;
using Telemart.Client.TransferObjects.HotlineCompetitor;
using Telemart.Client.ViewModels.Store.Order.ProductInformation;

namespace Telemart.Client.AutoMappings.Profiles
{
    public class HotlineMappingProfile : Profile
    {
        public HotlineMappingProfile()
        {
            CreateMap<HotlineCompetitorDto, HotlineCompetitorViewItem>();
        }
    }
}
