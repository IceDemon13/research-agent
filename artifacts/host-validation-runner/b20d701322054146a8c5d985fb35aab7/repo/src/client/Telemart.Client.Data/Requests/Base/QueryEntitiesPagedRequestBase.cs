using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using Telemart.Client.Data.WebClient;
using Telemart.Client.TransferObjects.Paging;

namespace Telemart.Client.Data.Requests.Base
{
    public abstract class QueryEntitiesPagedRequestBase<T> : RestClientGatewayRequestBase<PagedResult<T>>
        where T : class, new()
    {
        protected QueryEntitiesPagedRequestBase(IFilteringItem filter, params object[] pathParameters)
            : this(pathParameters)
        {
            if (filter is PagingFilteringItem pagingFilteringItem)
            {
                Skip = pagingFilteringItem.Skip;
                Take = pagingFilteringItem.Take;
            }

            UrlParameters = filter?.BuildParameters();

            Skip ??= GetIntValue(UrlParameters, PagingInfo.SkipName);
            Take ??= GetIntValue(UrlParameters, PagingInfo.TakeName);
        }

        protected QueryEntitiesPagedRequestBase(params object[] pathParameters)
            : base(HttpMethod.Get)
        {
            PathParameters = pathParameters;
        }

        public int? Skip { get; protected set; }

        public int? Take { get; protected set; }

        public override HttpRequestMessage BuildRequest()
        {
            Skip ??= GetIntValue(UrlParameters, PagingInfo.SkipName);
            Take ??= GetIntValue(UrlParameters, PagingInfo.TakeName);

            if (Skip != null || Take != null)
            {
                UrlParameters = (UrlParameters?.Where(x => x.Name != PagingInfo.SkipName && x.Name != PagingInfo.TakeName) ?? Enumerable.Empty<(string Name, object Value)>())
                    .Union(SkipTakeUrlParameters())
                    .ToArray();
            }

            return base.BuildRequest();
        }

        private IEnumerable<(string Name, object Value)> SkipTakeUrlParameters()
        {
            if (Skip.HasValue)
            {
                yield return (PagingInfo.SkipName, Skip);
            }

            if (Take.HasValue)
            {
                yield return (PagingInfo.TakeName, Take);
            }
        }

        private int? GetIntValue(IEnumerable<(string Name, object Value)> enumerable, string name)
        {
            if (enumerable == null)
            {
                return null;
            }

            (_, object value) = enumerable.FirstOrDefault(x => x.Name == name);

            if (value != null)
            {
                if (value is int intValue)
                {
                    return intValue;
                }
                else if (int.TryParse(value.ToString(), out int parsedIntValue))
                {
                    return parsedIntValue;
                }
            }

            return null;
        }
    }
}