using System.Linq;
using AutoMapper;
using DevExpress.Mvvm.Native;
using Telemart.Client.Extensions;
using Telemart.Client.TransferObjects.Cashbox;
using Telemart.Client.ViewModels.Cashbox;

namespace Telemart.Client.AutoMappings.Profiles
{
    public class CashboxMappingProfile : Profile
    {
        public CashboxMappingProfile()
        {
            CreateMap<CashboxDto, CashboxViewItem>()
                .ForMember(x => x.Employee, x => x.Ignore())
                .ForMember(x => x.ConnectTypeId, x => x.Ignore())
                .ForMember(x => x.EmployeeLockName, y => y.MapFrom(z => z.EmployeeLock.Name))
                .ForMember(x => x.AllowedPayments, y => y.MapFrom(z => z.AllowedPayments.ToObservableCollection()));

            CreateMap<CashboxViewItem, CashboxSaveDto>()
                .ForMember(x => x.AllowedPayments, y => y.MapFrom(z => z.AllowedPayments.ToArray()))
                .Ignore(x => x.IsCreateEmployeeCashbox);

            CreateMap<CashboxEditParameter, CashboxViewItem>(MemberList.Source)
                .ForSourceMember(x => x.IsNew, x => x.DoNotValidate());
        }
    }
}