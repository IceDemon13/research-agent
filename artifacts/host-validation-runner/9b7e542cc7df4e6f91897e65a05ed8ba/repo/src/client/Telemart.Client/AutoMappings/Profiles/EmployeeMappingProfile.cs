using AutoMapper;
using DevExpress.Mvvm.Native;
using Telemart.Client.AutoMappings.ValueResolvers;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.WorkAccount;
using Telemart.Client.ViewModels.Directories.Employee;

namespace Telemart.Client.AutoMappings.Profiles
{
    public sealed class EmployeeMappingProfile : Profile
    {
        public EmployeeMappingProfile()
        {
            CreateMap<EmployeeDto, EmployeeViewItem>()
                .ConstructUsing(x => EmployeeViewItem.Create())
                .ForMember(x => x.EmployeeLockName, y => y.MapFrom(z => z.EmployeeLock.ShortName))
                .ForMember(x => x.Subdivision, y => y.MapFrom<SubdivisionResolver, int>(z => z.SubdivisionId));

            CreateMap<EmployeeRichDto, EmployeeViewItem>()
                .ConstructUsing(x => EmployeeViewItem.Create())
                .ForMember(x => x.EmployeeLockName, y => y.MapFrom(z => z.EmployeeLock.ShortName))
                .ForMember(x => x.Subdivision, y => y.MapFrom<SubdivisionResolver, int>(z => z.SubdivisionId));

            CreateMap<EmployeeRichDto, EmployeeRichViewItem>()
                .ForMember(x => x.EmployeeLockName, y => y.MapFrom(z => z.EmployeeLock.ShortName))
                .ForMember(x => x.Roles, y => y.MapFrom(z => z.Roles.ToObservableCollection()))
                .ForMember(x => x.AllowSubdivisions, y => y.MapFrom(z => z.AllowSubdivisions.ToObservableCollection()))
                .ForMember(x => x.AllowWarehouses, y => y.MapFrom(z => z.AllowWarehouses.ToObservableCollection()))
                .ForMember(x => x.AllowCashboxes, y => y.MapFrom(z => z.AllowCashboxes.ToObservableCollection()))
                .ForMember(x => x.AllowCategories, y => y.MapFrom(z => z.AllowCategories.ToObservableCollection()));

            CreateMap<EmployeeAccountDto, EmployeeAccountViewItem>()
                .ForMember(x => x.Account, x => x.Ignore());
            CreateMap<EmployeeOperationDto, EmployeeOperationViewItem>();
        }
    }
}