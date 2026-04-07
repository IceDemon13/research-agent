using AutoMapper;
using Telemart.Client.TransferObjects.BankPayment;
using Telemart.Client.ViewModels.Money.Receive.BankPayment;

namespace Telemart.Client.AutoMappings.Profiles
{
    public class BankPaymentMappingProfile : Profile
    {
        public BankPaymentMappingProfile()
        {
            CreateMap<BankPaymentDto, BankPaymentViewItem>()
                .ForMember(x => x.ErrorMessage, x => x.Ignore())
                .ForMember(x => x.IsProcessed, x => x.Ignore());
        }
    }
}