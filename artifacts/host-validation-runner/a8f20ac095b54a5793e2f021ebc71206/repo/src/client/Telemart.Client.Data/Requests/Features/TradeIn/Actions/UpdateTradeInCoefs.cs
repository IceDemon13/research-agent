using System.Collections.Generic;
using Newtonsoft.Json;
using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects.TradeIn;

namespace Telemart.Client.Data.Requests.Features.TradeIn.Actions
{
    public sealed class UpdateTradeInCoefs : CallActionWithBodyRequestResultBase<object, UpdateTradeInCoefs.UpdateTradeInCoeffsDto>
    {
        public UpdateTradeInCoefs(IReadOnlyCollection<TradeInCoefDto> dtos)
            : base(new UpdateTradeInCoeffsDto(dtos), ApiResources.TradeIns, "coefs")
        {
        }

        public sealed record UpdateTradeInCoeffsDto
        {
            public UpdateTradeInCoeffsDto(IReadOnlyCollection<TradeInCoefDto> coeffs)
            {
                Coeffs = coeffs;
            }

            [JsonProperty("coeffs")]
            public IReadOnlyCollection<TradeInCoefDto> Coeffs { get; init; }
        }
    }
}