using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.Carry
{
    public class CarryPriceEditParameter : EditorParameter
    {
        public CarryPriceEditParameter(int carryId, int id)
            : base(id)
        {
            CarryId = carryId;
        }

        public int CarryId { get; }
    }
}
