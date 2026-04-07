using AutoMapper;
using Telemart.Client.AutoMappings.ValueResolvers;
using Telemart.Client.Business.Order;
using Telemart.Client.Dictionaries;
using Telemart.Client.TransferObjects;
using Telemart.Client.ViewModels.Money.Refund;

namespace Telemart.Client.AutoMappings.Profiles
{
    public sealed class RefundMappingProfile : Profile
    {
        public RefundMappingProfile()
        {
            CreateMap<RefundDto, RefundViewItem>()
                .ForMember(x => x.RealCompletedOnFiscalRegistrar, z => z.MapFrom(x => !string.IsNullOrWhiteSpace(x.FiscalId)))
                .ForMember(x => x.OrderStateId, x => x.Ignore())
                .ForMember(x => x.Fio, x => x.MapFrom(z => Fio.CreateFioString(z.LastName, z.FirstName, z.MiddleName)))
                .ForMember(x => x.State, y => y.MapFrom<DictionaryItemValueResolver<RefundState>, int>(z => z.StateId))
                .ForMember(x => x.Payment, y => y.MapFrom<DictionaryItemValueResolver<Payment>, int>(z => z.PaymentId ?? -1))
                .ForMember(x => x.Currency, y => y.MapFrom<CurrencyTypeResolver, int>(z => z.CurrencyId))
                .ForMember(x => x.Description, y => y.MapFrom(z => z.Description.Replace("\r\n", " ").Replace("\n", " ")))
                .ForMember(x => x.CashboxNotValid, y => y.Ignore());

            CreateMap<RefundViewItem, RefundSaveDto>()
                .ForMember(x => x.RefundRequisites, x => x.Ignore())
                .ForMember(x => x.PaymentId, y => y.MapFrom(z => z.Payment.Id));
        }
    }
}