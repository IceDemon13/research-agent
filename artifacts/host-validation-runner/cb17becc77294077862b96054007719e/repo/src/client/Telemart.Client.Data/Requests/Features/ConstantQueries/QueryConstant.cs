using Telemart.Client.Data.Requests.Base;

namespace Telemart.Client.Data.Requests.Features.ConstantQueries
{
    public sealed class QueryConstant : QueryEntityRequestBase<object>
    {
        public QueryConstant(object key)
            : base(ApiResources.Constants, key)
        {
        }
    }
}