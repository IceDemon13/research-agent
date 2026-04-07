using System;
using System.Collections.Generic;
using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Movement
{
    public class DelayMovement : CallEntityActionWithBodyRequestResultBase<MovementDto, MovementDelayDto>
    {
        public DelayMovement(int id, DateTime? sendDate, DateTime? arriveDate, DateTime? receiveDate, IReadOnlyCollection<MovementDelayProductDto> products, int expireReasonId, int? warehouseId)
            : base(id, new MovementDelayDto(id, products, sendDate, arriveDate, receiveDate, expireReasonId, warehouseId), ApiResources.Movements, "delay")
        {
        }
    }
}