using System.Net.Http;

namespace Telemart.Client.Data.Requests.Base
{
    public class QueryEntityRequestBase<T> : RestClientGatewayRequestBase<T>
        where T : class
    {
        public QueryEntityRequestBase(params object[] pathParameters)
            : base(HttpMethod.Get)
        {
            PathParameters = pathParameters;
        }
    }
}