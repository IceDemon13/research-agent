using System.Collections.Generic;
using Newtonsoft.Json;
using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Movement.Actions
{
    public class CanFastMovementProccesing : CallActionWithBodyRequestBase<IReadOnlyCollection<ValidationResultItemDto>, CanFastMovementProccesing.CanFastMovementProccesingDto>
    {
        public CanFastMovementProccesing(IReadOnlyCollection<MovementProductSaveDto> products)
            : base(new CanFastMovementProccesingDto(products), ApiResources.Movements, "can_fast_movement_proccesing")
        {
        }

        public class CanFastMovementProccesingDto
        {
            public CanFastMovementProccesingDto(IReadOnlyCollection<MovementProductSaveDto> products)
            {
                Products = products;
            }

            [JsonProperty("products")]
            public IReadOnlyCollection<MovementProductSaveDto> Products { get; }
        }
    }
}