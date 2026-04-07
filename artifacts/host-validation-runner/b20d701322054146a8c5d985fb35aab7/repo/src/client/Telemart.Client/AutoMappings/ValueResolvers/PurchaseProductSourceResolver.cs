using System;
using AutoMapper;
using Telemart.Client.Dictionaries;
using Telemart.Client.TransferObjects;
using Telemart.Client.ViewModels.Store;

namespace Telemart.Client.AutoMappings.ValueResolvers
{
    public sealed class PurchaseProductSourceResolver : IValueResolver<PurchaseDto, PurchaseViewItem, OrderProductSource>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PurchaseProductSourceResolver"/> class.
        /// </summary>
        /// <param name="dictionaries">The dictionaries.</param>
        /// <exception cref="ArgumentNullException"><paramref name="dictionaries"/> is <see langword="null" />.</exception>
        public PurchaseProductSourceResolver(IDictionaries dictionaries)
        {
            Dictionaries = dictionaries ?? throw new ArgumentNullException(nameof(dictionaries));
        }

        private IDictionaries Dictionaries { get; }

        public OrderProductSource Resolve(PurchaseDto source, PurchaseViewItem destination, OrderProductSource destMember, ResolutionContext context)
        {
            return Dictionaries.GetOrderProductSource(
                source.ProductSourceId,
                source.ProductWarehouseId,
                source.ProductSourceText,
                source.ProductSourceDate);
        }
    }
}
