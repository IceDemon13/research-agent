using System;
using Telemart.Client.Data.Requests.Base;
using Telemart.Client.Data.WebClient;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.ModuleAnalyticUrl
{
    public class QueryModuleAnalyticUrls : QueryEntitiesRequestBase<ModuleAnalyticUrlDto>
    {
        public QueryModuleAnalyticUrls()
            : base(ApiResources.ModuleAnalyticUrls)
        {
        }

        public QueryModuleAnalyticUrls(DateTime? modifiedOnAfter)
            : base(new ModifiedOnAfterFilteringItem { ModifiedOnAfter = modifiedOnAfter }, ApiResources.ModuleAnalyticUrls)
        {
        }
    }
}