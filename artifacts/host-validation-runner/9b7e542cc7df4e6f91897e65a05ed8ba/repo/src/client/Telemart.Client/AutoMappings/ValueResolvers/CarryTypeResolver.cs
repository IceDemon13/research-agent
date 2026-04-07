using System;
using AutoMapper;
using Telemart.Client.Dictionaries;

namespace Telemart.Client.AutoMappings.ValueResolvers
{
    public sealed class CarryTypeResolver : IMemberValueResolver<object, object, int, CarryType>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CarryTypeResolver"/> class.
        /// </summary>
        /// <param name="dictionaries">The dictionaries.</param>
        /// <exception cref="ArgumentNullException"><paramref name="dictionaries" /> is <see langword="null" />.</exception>
        public CarryTypeResolver(IDictionaries dictionaries)
        {
            Dictionaries = dictionaries ?? throw new ArgumentNullException(nameof(dictionaries));
        }

        private IDictionaries Dictionaries { get; }

        public CarryType Resolve(object source, object destination, int sourceMember, CarryType destMember, ResolutionContext context)
        {
            return Dictionaries.GetItemById<CarryType>(sourceMember);
        }
    }
}