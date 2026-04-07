using System.Net.Http;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.Data.Requests.Base
{
    public abstract class UpdateCommentBase<T> : RestClientGatewayRequestBase<Result<T>>
        where T : class
    {
        protected UpdateCommentBase(int id, string resource, string comment)
            : base(HttpMethod.Put)
        {
            Body = new { id = id, comment = comment };

            PathParameters = new object[] { resource, id, "comment" };
        }
    }
}