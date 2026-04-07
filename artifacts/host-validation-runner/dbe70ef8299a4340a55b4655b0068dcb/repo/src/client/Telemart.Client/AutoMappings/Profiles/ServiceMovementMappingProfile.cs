using System.Linq;
using AutoMapper;
using Telemart.Client.AutoMappings.ValueResolvers;
using Telemart.Client.Dictionaries;
using Telemart.Client.TransferObjects.ServiceMovement;
using Telemart.Client.ViewModels.Service.ServiceMovements;

namespace Telemart.Client.AutoMappings.Profiles
{
    public class ServiceMovementMappingProfile : Profile
    {
        public ServiceMovementMappingProfile()
        {
            CreateMap<ServiceMovementProductDto, ServiceMovementProductViewItem>()
                .ForMember(x => x.IsProcessed, x => x.Ignore())
                .ForMember(x => x.IsProcessing, x => x.Ignore())
                .ForMember(x => x.Scanned, x => x.Ignore())
                .ForMember(x => x.ErrorMessage, x => x.Ignore())
                .ForMember(x => x.Price, x => x.Ignore())
                .ForMember(x => x.CurrencyId, x => x.Ignore())
                .ForMember(x => x.MovementState, x => x.Ignore())
                .ConstructUsing(x => new ServiceMovementProductViewItem());
            CreateMap<ServiceMovementSimpleDto, ServiceMovementViewItem>()
                .ForMember(x => x.EmployeeLockId, x => x.Ignore())
                .ForMember(x => x.EmployeeLockName, x => x.Ignore())
                .ForMember(x => x.Products, x => x.Ignore())
                .ForMember(x => x.DeliveryTypeId, x => x.Ignore())
                .ForMember(x => x.Places, x => x.Ignore())
                .ForMember(x => x.State, y => y.MapFrom<DictionaryItemValueResolver<MovementState>, int>(z => z.StateId))
                .ForMember(x => x.Carry, y => y.MapFrom<DictionaryItemValueResolver<CarryType>, int>(z => z.CarryId));
            CreateMap<ServiceMovementDto, ServiceMovementViewItem>()
                .ForMember(x => x.State, y => y.MapFrom<DictionaryItemValueResolver<MovementState>, int>(z => z.StateId))
                .ForMember(x => x.Carry, y => y.MapFrom<DictionaryItemValueResolver<CarryType>, int>(z => z.CarryId));
            CreateMap<ServiceMovementCreateViewModel, ServiceMovementCreateDto>()
                .ForMember(x => x.Places, x => x.Ignore())
                .ForMember(x => x.ServiceRequestIds, y => y.MapFrom(z => z.Products.Select(x => x.ServiceRequestId).ToArray()));
        }
    }
}