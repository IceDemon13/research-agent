using System.Net;
using System.Net.Http;

namespace Telemart.Client.Data.Requests.Base
{
    public abstract class DeleteEntityRequestBase<TResult> : RestClientGatewayRequestBase<TResult>
        where TResult : class
    {
        protected DeleteEntityRequestBase(params object[] pathParameters)
        : base(HttpMethod.Delete)
        {
            PathParameters = pathParameters;
        }
    }

    public abstract class DeleteEntityRequestBase : DeleteEntityRequestBase<object>
    {
        protected DeleteEntityRequestBase(params object[] pathParameters)
            : base(pathParameters)
        {
            SuccessStatusCode = HttpStatusCode.NoContent;
        }
    }
}