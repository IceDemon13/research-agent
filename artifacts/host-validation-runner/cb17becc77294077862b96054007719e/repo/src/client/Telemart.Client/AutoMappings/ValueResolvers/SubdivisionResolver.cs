using System;
using AutoMapper;
using Telemart.Client.Dictionaries;

namespace Telemart.Client.AutoMappings.ValueResolvers
{
    public sealed class SubdivisionResolver : IMemberValueResolver<object, object, int, Subdivision>
    {
        public SubdivisionResolver(IDictionaries dictionaries)
        {
            Dictionaries = dictionaries ?? throw new ArgumentNullException(nameof(dictionaries));
        }

        private IDictionaries Dictionaries { get; }

        public Subdivision Resolve(object source, object destination, int sourceMember, Subdivision destMember, ResolutionContext context)
        {
            return Dictionaries.GetItemById<Subdivision>(sourceMember);
        }
    }
}