using System.Collections.Generic;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.ViewModels.Warehouse.Movement
{
    public sealed class MovementsMassScanParameter
    {
        public MovementsMassScanParameter(IReadOnlyCollection<MovementDto> movements)
        {
            Movements = movements;
        }

        public IReadOnlyCollection<MovementDto> Movements { get; }
    }
}