using System;
using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.TradeIn;

namespace Telemart.Client.Data.Requests.Features.TradeIn
{
    public sealed class CreateTradeInTrackNumber : CallEntityActionWithBodyRequestResultBase<TradeInCreateTrackNumberResultDto, TradeInCreateTrackNumberDto>
    {
        public CreateTradeInTrackNumber(
            int id,
            string recipient,
            string phone,
            int carryId,
            DeliveryDataDto deliveryData,
            int places,
            double weight,
            decimal insurance,
            bool addToApplication,
            int? packageWidth,
            int? packageLength,
            int? packageHeight)
        : base(
            id,
            new TradeInCreateTrackNumberDto(id, recipient, phone, carryId, deliveryData, places, weight, insurance, addToApplication, packageWidth, packageLength, packageHeight),
            ApiResources.TradeIns,
            "create_tn")
        {
        }
    }
}