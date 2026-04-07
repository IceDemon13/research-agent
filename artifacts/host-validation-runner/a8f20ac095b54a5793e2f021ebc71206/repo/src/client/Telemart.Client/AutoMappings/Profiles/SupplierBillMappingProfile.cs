using AutoMapper;
using Telemart.Client.AutoMappings.ValueResolvers;
using Telemart.Client.Dictionaries;
using Telemart.Client.TransferObjects.SupplierBill;
using Telemart.Client.ViewModels.SupplierBill;

namespace Telemart.Client.AutoMappings.Profiles
{
    public class SupplierBillMappingProfile : Profile
    {
        public SupplierBillMappingProfile()
        {
            CreateMap<SupplierBillDto, SupplierBillViewItem>()
                .ForMember(x => x.EmployeeLockName, y => y.MapFrom(z => z.EmployeeLock.Name));
            CreateMap<SupplierBillDocumentDto, SupplierBillDocumentViewItem>();
            CreateMap<SupplierBillProductDto, SupplierBillProductViewItem>()
                .ForMember(x => x.Name, y => y.MapFrom(z => z.ProductNameRu))
                .ForMember(x => x.NameUkr, y => y.MapFrom(z => z.ProductNameUa))
                .ForMember(x => x.NameEn, y => y.MapFrom(z => z.ProductNameEn))
                .ForMember(x => x.TaxRate, y => y.MapFrom<DictionaryItemValueResolver<TaxRate>, int>(z => z.TaxRateId));
        }
    }
}