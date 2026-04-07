using System.Net.Http;

namespace Telemart.Client.Data.Requests.Base.Action
{
    public abstract class CallActionRequestBase<T> : RestClientGatewayRequestBase<T>
        where T : class
    {
        protected CallActionRequestBase(string resource, string methodName)
            : base(HttpMethod.Post)
        {
            PathParameters = new[] { resource, "actions", methodName };
        }
    }
}