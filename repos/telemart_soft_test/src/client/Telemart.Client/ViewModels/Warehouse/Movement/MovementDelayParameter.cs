using System;

namespace Telemart.Client.ViewModels.Warehouse.Movement
{
    public class MovementDelayParameter
    {
        public MovementDelayParameter(int movementId, DateTime date, int stateId, int? warehouseId)
        {
            MovementId = movementId;
            Date = date;
            StateId = stateId;
            WarehouseId = warehouseId;
        }

        public int MovementId { get; }

        public DateTime Date { get; }

        public int StateId { get; }

        public int? WarehouseId { get; }
    }
}