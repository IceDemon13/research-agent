using System.Collections.Generic;
using System.Net.Http;
using Telemart.Client.Data.WebClient;

namespace Telemart.Client.Data.Requests.Base
{
    public abstract class QueryEntitiesRequestBase<T> : RestClientGatewayRequestBase<List<T>>
        where T : class, new()
    {
        protected QueryEntitiesRequestBase(IFilteringItem filter, params object[] pathParameters)
            : this(pathParameters)
        {
            UrlParameters = filter?.BuildParameters();
        }

        protected QueryEntitiesRequestBase(params object[] pathParameters)
            : base(HttpMethod.Get)
        {
            PathParameters = pathParameters;
        }
    }
}