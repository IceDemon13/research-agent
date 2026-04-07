using Telemart.Client.Data.Requests.Base;

namespace Telemart.Client.Data.Requests.Features.Setting
{
    public sealed class QueryEmployeeCashBoxLegalEntityId : QueryEntityRequestBase<string>
    {
        public QueryEmployeeCashBoxLegalEntityId()
            : base(ApiResources.Settings, "employee_cashbox_legal_entity_id")
        {
        }
    }
}