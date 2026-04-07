using System;
using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.CustomerBonus
{
    public sealed class QueryExpireDate : QueryRequestBase<ExpireDateDto>
    {
        public QueryExpireDate(int bonusTypeId, DateTime? bonusDate)
        : base(ApiResources.CustomerBonuses, "expire_date", bonusTypeId)
        {
            if (bonusDate.HasValue)
            {
                UrlParameters = new (string Name, object Value)[] { ("bonusDate", bonusDate.Value.ToString("s")) };
            }
        }
    }
}