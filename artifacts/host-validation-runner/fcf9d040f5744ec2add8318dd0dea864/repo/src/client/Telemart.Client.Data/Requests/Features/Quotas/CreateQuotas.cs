using System;
using System.Collections.Generic;
using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects.Quotas;

namespace Telemart.Client.Data.Requests.Features.Quotas
{
    public sealed class CreateQuotas : CallActionWithBodyRequestResultBase<List<QuotaDto>, QuotasCreateDto>
    {
        public CreateQuotas(DateTime? quotasDate, QuotaCreateDto[] quotas)
        : base(new QuotasCreateDto(quotasDate, quotas), ApiResources.Quotas, "create_quotas")
        {
        }
    }
}