using System.Net.Http;

namespace Telemart.Client.Data.Requests.Base.Action;

public abstract class CallEntityActionRequestBase<T> : RestClientGatewayRequestBase<T>
    where T : class
{
    protected CallEntityActionRequestBase(object id, string resource, string methodName)
        : base(HttpMethod.Post)
    {
        PathParameters = new[] { resource, id, "actions", methodName };
    }
}