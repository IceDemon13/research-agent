using System.Collections.Generic;
using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Parser.Actions
{
    public sealed class UpdateProductsParserAliases : CallActionWithBodyRequestBase<List<ParserAliasDto>, IReadOnlyCollection<ParserAliasSaveDto>>
    {
        public UpdateProductsParserAliases(IReadOnlyCollection<ParserAliasSaveDto> parserAliasSaveDtos)
            : base(parserAliasSaveDtos, "parser/products/aliases", "save")
        {
        }
    }
}