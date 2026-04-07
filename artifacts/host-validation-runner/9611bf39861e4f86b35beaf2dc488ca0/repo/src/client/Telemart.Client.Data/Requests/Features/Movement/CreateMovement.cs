using System;
using System.Collections.Generic;
using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Movement
{
    public sealed class CreateMovement : CreateEntityResultRequestBase<MovementDto, MovementCreateDto>
    {
        public CreateMovement(
            int warehouseFrom,
            int warehouseTo,
            DateTime dateOut,
            DateTime dateDeparture,
            DateTime dateArrive,
            DateTime dateIn,
            bool moveFreeStocks,
            bool createEmpty,
            int? carryId,
            int? deliveryTypeId,
            IReadOnlyCollection<int> purposeIds,
            bool checkExists = true,
            bool system = false,
            bool ignoreDateOutValidation = false)
            : base(
                new MovementCreateDto(
                    warehouseFrom,
                    warehouseTo,
                    dateOut,
                    dateDeparture,
                    dateArrive,
                    dateIn,
                    moveFreeStocks,
                    createEmpty,
                    carryId,
                    deliveryTypeId,
                    purposeIds,
                    checkExists,
                    system,
                    ignoreDateOutValidation),
                ApiResources.Movements)
        {
        }
    }
}