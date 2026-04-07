using AutoMapper;
using Telemart.Client.AutoMappings.ValueResolvers;
using Telemart.Client.Dictionaries;
using Telemart.Client.TransferObjects;
using Telemart.Client.ViewModels.Service.ServiceInvoices;

namespace Telemart.Client.AutoMappings.Profiles
{
    public sealed class ServiceInvoiceMappingProfile : Profile
    {
        public ServiceInvoiceMappingProfile()
        {
            CreateMap<ServiceInvoiceDto, ServiceInvoiceViewItem>()
                .ForMember(x => x.State, y => y.MapFrom<DictionaryItemValueResolver<ServiceInvoiceState>, int>(z => z.StateId))
                .ForMember(x => x.CarryType, y => y.MapFrom<DictionaryItemValueResolver<CarryType>, int>(z => z.CarryId));

            CreateMap<ServiceInvoiceProductDto, ServiceInvoiceProductViewItem>()
                .ForMember(x => x.Accepted, x => x.Ignore())
                .ForMember(x => x.State, y => y.MapFrom<DictionaryItemValueResolver<ServiceInvoiceProductState>, int>(z => z.StateId))
                .ForMember(x => x.Defect, y => y.MapFrom(z => z.Defect.Replace("\r\n", " ").Replace("\n", " ")));

            CreateMap<ServiceInvoiceProductViewItem, ServiceInvoiceProductDto>()
                .ForMember(x => x.StateId, y => y.MapFrom(z => z.State.Id));

            CreateMap<ServiceInvoiceProductViewItem, RepairInvoiceDto>()
                .ForMember(x => x.ServiceInvoiceProductId, y => y.MapFrom(z => z.Id));
        }
    }
}