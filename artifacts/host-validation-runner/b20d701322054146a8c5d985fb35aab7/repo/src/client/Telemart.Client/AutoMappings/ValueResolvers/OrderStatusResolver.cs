using System;
using AutoMapper;
using Telemart.Client.Dictionaries;

namespace Telemart.Client.AutoMappings.ValueResolvers
{
    public sealed class OrderStatusResolver : IMemberValueResolver<object, object, int, OrderStatus>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="OrderStatusResolver"/> class.
        /// </summary>
        /// <param name="dictionaries">The dictionaries.</param>
        /// <exception cref="ArgumentNullException"><paramref name="dictionaries" /> is <see langword="null" />.</exception>
        public OrderStatusResolver(IDictionaries dictionaries)
        {
            Dictionaries = dictionaries ?? throw new ArgumentNullException(nameof(dictionaries));
        }

        private IDictionaries Dictionaries { get; }

        public OrderStatus Resolve(object source, object destination, int sourceMember, OrderStatus destMember, ResolutionContext context)
        {
            return Dictionaries.GetItemById<OrderStatus>(sourceMember);
        }
    }
}