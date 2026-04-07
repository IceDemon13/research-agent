using System.Collections.Generic;
using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Prices
{
    public sealed class CalculateExtraCharge : CallActionWithBodyRequestResultBase<IReadOnlyCollection<ContractorProductExtraChargeDto>, CalculateExtraChargeDto>
    {
        public CalculateExtraCharge(CalculateExtraChargeDto dto)
            : base(dto, ApiResources.Prices, "calculate_extra_charge")
        {
        }
    }
}