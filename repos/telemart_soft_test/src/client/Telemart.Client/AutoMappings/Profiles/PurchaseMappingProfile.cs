using AutoMapper;
using Telemart.Client.AutoMappings.ValueResolvers;
using Telemart.Client.Dictionaries;
using Telemart.Client.Helpers;
using Telemart.Client.TransferObjects;
using Telemart.Client.ViewModels.Store;

namespace Telemart.Client.AutoMappings.Profiles
{
    public sealed class PurchaseMappingProfile : Profile
    {
        public PurchaseMappingProfile()
        {
            CreateMap<PurchaseDto, PurchaseViewItem>()
                .ForMember(x => x.Категория, x => x.Ignore())
                .ForMember(x => x.OrderEmployeeCreateId, x => x.Ignore())
                .ForMember(x => x.PurchaseEmployeeType, x => x.Ignore())
                .ConstructUsing(x => PurchaseViewItem.Create())
                .ForMember(x => x.OrderComment, x => x.MapFrom(z => OrderCommentHelper.GetJoinedComment(z.CustomerComment, z.EmployeeComment, z.SystemComment)))
                .ForMember(x => x.ProductState, y => y.MapFrom<OrderProductStatusResolver, int>(z => z.ProductStateId))
                .ForMember(x => x.ProductSource, y => y.MapFrom<PurchaseProductSourceResolver>())
                .ForMember(x => x.OrderCarryType, y => y.MapFrom<CarryTypeResolver, int>(z => z.OrderCarryId))
                .ForMember(x => x.OrderPayment, y => y.MapFrom<DictionaryItemValueResolver<Payment>, int>(z => z.OrderPaymentId))
                .ForMember(x => x.OrderState, y => y.MapFrom<OrderStatusResolver, int>(z => z.OrderStateId))
                .ForMember(x => x.OrderSubdivision, y => y.MapFrom<SubdivisionResolver, int>(z => z.OrderSubdivisionId));
        }
    }
}