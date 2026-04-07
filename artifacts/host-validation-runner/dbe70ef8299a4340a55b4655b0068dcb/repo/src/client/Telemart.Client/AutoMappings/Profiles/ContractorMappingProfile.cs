using System;
using System.Linq;
using AutoMapper;
using Telemart.Client.AutoMappings.ValueResolvers;
using Telemart.Client.Common;
using Telemart.Client.Common.MvvmEnhancements;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.TransferObjects;
using Telemart.Client.ViewModels.Directories.Contractor;

namespace Telemart.Client.AutoMappings.Profiles
{
    public sealed class ContractorMappingProfile : Profile
    {
        public ContractorMappingProfile()
        {
            // contractor
            CreateMap<ContractorDto, ContractorViewItem>()
                .ForMember(x => x.Employee, x => x.Ignore())
                .ForMember(x => x.Subdivision, y => y.MapFrom<SubdivisionResolver, int>(z => z.SubdivisionId))
                .ForMember(x => x.AbcType, y => y.MapFrom<DictionaryItemValueResolver<AbcType>, int>(z => z.AbcId))
                .ForMember(x => x.CityId, y => y.MapFrom(z => z.CountryId == null || z.CountryId == Constants.UkraineCountryId ? z.CityId ?? -1 : z.ForeignCityId))
                .ForMember(x => x.EmployeeLockName, y => y.MapFrom(z => z.EmployeeLock.ShortName))
                .ForMember(x => x.PurchaseProcessorName, y => y.MapFrom(z => z.Purchase.ProcessorName))
                .ForMember(x => x.PurchaseLogin, y => y.MapFrom(z => z.Purchase.Login))
                .ForMember(x => x.PurchasePassword, y => y.MapFrom(z => z.Purchase.Password))
                .ForMember(x => x.PurchaseAutoReserve, y => y.MapFrom(z => z.Purchase.AutoReserve))
                .ForMember(x => x.PurchaseAutoPurchase, y => y.MapFrom(z => z.Purchase.AutoPurchase))
                .ForMember(x => x.PurchaseCheckUnique, y => y.MapFrom(z => z.Purchase.CheckUnique));

            // contact
            CreateMap<ContractorContactDto, ContractorContactViewItem>()
                .ForMember(x => x.CityName, x => x.Ignore())
                .ForMember(x => x.Positions, y => y.MapFrom(z => z.Positions.Select(p => new ComboBoxItem(p.Id, p.Name, p.Active, null)).ToList()))
                .ForMember(x => x.CityId, y => y.MapFrom(z => z.CountryId == null || z.CountryId == Constants.UkraineCountryId ? z.CityId : z.ForeignCityId));
            CreateMap<ContractorContactViewItem, ContractorContactSaveDto>()
                .ForMember(x => x.Password, x => x.Ignore())
                .ForMember(x => x.Positions, y => y.MapFrom(z => z.Positions.Select(p => p.Id).ToArray()))
                .ForMember(x => x.CityId, y => y.MapFrom(z => (z.CountryId == null || z.CountryId == Constants.UkraineCountryId) && z.CityId > 0 ? z.CityId : null))
                .ForMember(x => x.ForeignCityId, y => y.MapFrom(z => z.CountryId.HasValue && z.CountryId != Constants.UkraineCountryId ? z.CityId : null));

            // warehouse
            CreateMap<SupplierWarehouseViewItem, SupplierWarehouseDto>()
                .ForMember(x => x.CityId, y => y.Condition(z => z.CityId > 0))
                .ForMember(x => x.SupplierWarehouseAvails, y => y.MapFrom(z => z.SupplierWarehouseAvails));
            CreateMap<SupplierWarehouseDto, SupplierWarehouseViewItem>()
                .ForMember(x => x.EmployeeLockId, x => x.Ignore())
                .ForMember(x => x.EmployeeLockName, x => x.Ignore())
                .ForMember(x => x.SupplierWarehouseAvails, y => y.MapFrom(z => z.SupplierWarehouseAvails));

            // carry
            CreateMap<SupplierCarryDto, SupplierCarryViewItem>()
                .ForMember(x => x.CarryType, y => y.MapFrom<CarryTypeResolver, int>(z => z.CarryId));
            CreateMap<SupplierCarryViewItem, SupplierCarrySaveDto>()
                .ForMember(x => x.WarehouseId, y => y.MapFrom(z => z.WarehouseId))
                .ForMember(x => x.CarryId, y => y.MapFrom(z => z.CarryType.Id))
                .ForMember(x => x.SupplierWarehouseId, y => y.Condition(z => z.SupplierWarehouseId > 0));

            // abc
            CreateMap<SupplierCategoryAbcDto, SupplierCategoryAbcViewItem>()
                .ConstructUsing(x => SupplierCategoryAbcViewItem.Create())
                .ForMember(x => x.AbcType, y => y.MapFrom<DictionaryItemValueResolver<AbcType>, int>(z => z.AbcId));
            CreateMap<SupplierCategoryAbcViewItem, SupplierCategoryAbcSaveDto>();

            CreateMap<ContractorCurrencyPermissionDto, ContractorCurrencyPermissionViewItem>();
        }
    }
}