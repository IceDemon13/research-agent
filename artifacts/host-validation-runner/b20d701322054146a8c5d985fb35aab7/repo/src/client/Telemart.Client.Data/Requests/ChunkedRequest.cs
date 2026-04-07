using Telemart.Client.Data.Requests.Base;

namespace Telemart.Client.Data.Requests
{
    public class ChunkedRequest<T> : QueryEntitiesPagedRequestBase<T>
        where T : class, new()
    {
        public ChunkedRequest(QueryEntitiesPagedRequestBase<T> request, int skip, int take)
        {
            PathParameters = request.PathParameters;
            UrlParameters = request.UrlParameters;
            Skip = skip;
            Take = take;
        }
    }
}