using System.Net.Http;

namespace Telemart.Client.Data.Requests.Base
{
    public class QueryRequestBase<T> : RestClientGatewayRequestBase<T>
        where T : class, new()
    {
        protected QueryRequestBase(params object[] pathParameters)
            : base(HttpMethod.Get)
        {
            PathParameters = pathParameters;
        }
    }
}