using System.Collections.Generic;
using System.Net;
using Telemart.Client.Data.Requests.Base.Action;

namespace Telemart.Client.Data.Requests.Features.Parser.Actions
{
    public sealed class DeleteProductsParserAliases : CallActionWithBodyRequestBase<object, IReadOnlyCollection<long>>
    {
        public DeleteProductsParserAliases(IReadOnlyCollection<long> ids)
            : base(ids, "parser/products/aliases", "delete")
        {
            SuccessStatusCode = HttpStatusCode.NoContent;
        }
    }
}