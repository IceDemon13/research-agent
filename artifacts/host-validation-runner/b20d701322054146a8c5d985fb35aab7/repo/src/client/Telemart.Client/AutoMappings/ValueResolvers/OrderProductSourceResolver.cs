using System;
using AutoMapper;
using Telemart.Client.Business.Order;
using Telemart.Client.Dictionaries;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.AutoMappings.ValueResolvers
{
    public sealed class OrderProductSourceResolver : IValueResolver<OrderProductSimpleDto, IOrderProduct, OrderProductSource>
    {
        public OrderProductSourceResolver(IDictionaries dictionaries)
        {
            Dictionaries = dictionaries ?? throw new ArgumentNullException(nameof(dictionaries));
        }

        private IDictionaries Dictionaries { get; }

        public OrderProductSource Resolve(OrderProductSimpleDto source, IOrderProduct destination, OrderProductSource destMember, ResolutionContext context)
        {
            return Dictionaries.GetOrderProductSource(
                 source.SourceId,
                 source.WarehouseId,
                 source.SourceText,
                 source.SourceDate);
        }
    }
}