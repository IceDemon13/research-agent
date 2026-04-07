using System.Collections.Generic;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Business.Parser
{
    public interface IComparsionManager
    {
        ComparsionResult Compare(ParserAliasDto parserAlias, IReadOnlyCollection<ProductComparsionDto> products);
    }
}