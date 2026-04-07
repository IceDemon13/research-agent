using AutoMapper;
using Telemart.Client.Dictionaries;

namespace Telemart.Client.AutoMappings.ValueResolvers
{
    public sealed class CurrencyTypeResolver : IMemberValueResolver<object, object, int, Currency>
    {
        public Currency Resolve(object source, object destination, int sourceMember, Currency destMember, ResolutionContext context)
        {
            return Currency.GetById(sourceMember);
        }
    }
}