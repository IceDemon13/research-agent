using System.Collections.Generic;
using System.Linq;
using AutoMapper;
using Telemart.Client.AutoMappings.ValueResolvers;
using Telemart.Client.Dictionaries;
using Telemart.Client.TransferObjects;
using Telemart.Client.ViewModels.Directories.Organization;

namespace Telemart.Client.AutoMappings.Profiles
{
    public sealed class OrganizationMappingProfile : Profile
    {
        public OrganizationMappingProfile()
        {
            CreateMap<OrganizationDto, OrganizationViewItem>()
                .ForMember(x => x.EmployeeLockName, y => y.MapFrom(z => z.EmployeeLock.ShortName))
                .ForMember(x => x.OrganizationOwnership, y => y.MapFrom<DictionaryItemValueResolver<OrganizationOwnership>, int>(z => z.OwnershipId));

            CreateMap<OrganizationViewItem, OrganizationSaveDto>();

            CreateMap<OrganizationAccountDto, OrganizationAccountViewItem>()
                .ForMember(x => x.Bank, x => x.Ignore())
                .ConstructUsing(x => OrganizationAccountViewItem.Create())
                .ForMember(x => x.Currency, y => y.MapFrom(z => Currency.GetById(z.CurrencyId)))
                .ForMember(x => x.Payments, y => y.MapFrom<DictionaryItemsToObservableCollectionValueResolver<Payment>, List<int>>(z => z.Payments));
            CreateMap<OrganizationAccountViewItem, OrganizationAccountDto>()
                .ForMember(x => x.Payments, y => y.MapFrom(z => z.Payments.Select(x => x.Id).ToList()));

            CreateMap<OrganizationContactDto, OrganizationContactViewItem>()
                .ConstructUsing(x => OrganizationContactViewItem.Create())
                .ForMember(x => x.Position, y => y.MapFrom<DictionaryItemValueResolver<OrganizationPosition>, int>(z => z.PositionId));
            CreateMap<OrganizationContactViewItem, OrganizationContactDto>();

            CreateMap<OrganizationAccountViewItem, OrganizationAccountSaveDto>()
                .ForMember(x => x.Payments, y => y.MapFrom(z => z.Payments.Select(x => x.Id).ToList()));
        }
    }
}