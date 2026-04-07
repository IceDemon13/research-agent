using System.Collections.Generic;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.ViewModels.Warehouse.Movement
{
    public sealed class SelectMovementsParameter
    {
        public SelectMovementsParameter(IReadOnlyCollection<MovementDto> movementsToSelect, string title)
        {
            MovementsToSelect = movementsToSelect;
            Title = title;
        }

        public IReadOnlyCollection<MovementDto> MovementsToSelect { get; }

        public string Title { get; }
    }
}