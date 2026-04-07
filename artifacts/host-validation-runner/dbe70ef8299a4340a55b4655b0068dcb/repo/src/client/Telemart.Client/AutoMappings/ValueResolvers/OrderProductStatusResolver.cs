using System;
using AutoMapper;
using Telemart.Client.Dictionaries;

namespace Telemart.Client.AutoMappings.ValueResolvers
{
    public sealed class OrderProductStatusResolver : IMemberValueResolver<object, object, int, OrderProductStatus>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="OrderProductStatusResolver"/> class.
        /// </summary>
        /// <param name="dictionaries">The dictionaries.</param>
        /// <exception cref="ArgumentNullException"><paramref name="dictionaries" /> is <see langword="null" />.</exception>
        public OrderProductStatusResolver(IDictionaries dictionaries)
        {
            Dictionaries = dictionaries ?? throw new ArgumentNullException(nameof(dictionaries));
        }

        private IDictionaries Dictionaries { get; }

        public OrderProductStatus Resolve(object source, object destination, int sourceMember, OrderProductStatus destMember, ResolutionContext context)
        {
            return Dictionaries.GetItemById<OrderProductStatus>(sourceMember);
        }
    }
}