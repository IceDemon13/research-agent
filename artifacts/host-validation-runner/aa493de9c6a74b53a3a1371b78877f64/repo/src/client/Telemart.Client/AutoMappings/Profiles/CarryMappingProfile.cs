using AutoMapper;
using Telemart.Client.TransferObjects.Carry;
using Telemart.Client.ViewModels.Carry;

namespace Telemart.Client.AutoMappings.Profiles
{
    public class CarryMappingProfile : Profile
    {
        public CarryMappingProfile()
        {
            CreateMap<CarryPriceDto, CarryPriceViewItem>()
                .ForMember(x => x.EmployeeLockId, x => x.Ignore())
                .ForMember(x => x.EmployeeLockName, x => x.Ignore());
            CreateMap<CarryPriceEditParameter, CarryPriceViewItem>(MemberList.Source)
                .ForSourceMember(x => x.CarryId, x => x.DoNotValidate())
                .ForSourceMember(x => x.IsNew, x => x.DoNotValidate());
        }
    }
}