using System;
using AutoMapper;
using Telemart.Client.Dictionaries;

namespace Telemart.Client.AutoMappings.ValueResolvers
{
    public sealed class DictionaryItemValueResolver<TDestMember> : IMemberValueResolver<object, object, int, TDestMember>
        where TDestMember : DictionaryItemBase
    {
        public DictionaryItemValueResolver(IDictionaries dictionaries)
        {
            Dictionaries = dictionaries ?? throw new ArgumentNullException(nameof(dictionaries));
        }

        private IDictionaries Dictionaries { get; }

        public TDestMember Resolve(object source, object destination, int sourceMember, TDestMember destMember, ResolutionContext context)
        {
            return Dictionaries.GetItemById<TDestMember>(sourceMember);
        }
    }
}
