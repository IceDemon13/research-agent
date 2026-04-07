using AutoMapper;
using Telemart.Client.AutoMappings.ValueResolvers;
using Telemart.Client.Dictionaries;
using Telemart.Client.TransferObjects.LogisticsAnalitics;
using Telemart.Client.ViewModels.LogisticsAnalitics;

namespace Telemart.Client.AutoMappings.Profiles
{
    public class LogisticsAnaliticsProfile : Profile
    {
        public LogisticsAnaliticsProfile()
        {
            CreateMap<LogisticsAnaliticsOrderDto, OrderLogisticsAnaliticsViewItem>()
                .ForMember(x => x.OrderId, y => y.MapFrom(z => z.Id))
                .ForMember(x => x.State, y => y.MapFrom<OrderStatusResolver, int>(z => z.StateId))
                .ForMember(x => x.Subdivision, y => y.MapFrom<SubdivisionResolver, int>(z => z.SubdivisionId))
                .ForMember(x => x.OrdersPriceString, y => y.Ignore());

            CreateMap<LogisticsAnaliticsMovementDto, MovementLogisticsAnaliticsViewItem>()
                .ForMember(x => x.MovementId, y => y.MapFrom(z => z.Id))
                .ForMember(x => x.State, y => y.MapFrom<DictionaryItemValueResolver<MovementState>, int>(z => z.StateId))
                .ForMember(x => x.DateRecievSent, y => y.Ignore())
                .ForMember(x => x.Type, y => y.Ignore())
                .ForMember(x => x.OrdersPriceString, y => y.Ignore());

            CreateMap<LogisticsAnaliticsInvoiceDto, InvoiceLogisticsAnaliticsViewItem>()
                .ForMember(x => x.InvoiceId, y => y.MapFrom(z => z.Id))
                .ForMember(x => x.State, y => y.MapFrom<DictionaryItemValueResolver<InvoiceState>, int>(z => z.StateId))
                .ForMember(x => x.OrdersPriceString, y => y.Ignore());

        }
    }
}