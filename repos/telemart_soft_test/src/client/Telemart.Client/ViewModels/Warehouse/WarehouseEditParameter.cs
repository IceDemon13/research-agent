using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.Warehouse
{
    public class WarehouseEditParameter : EditorParameter
    {
        public WarehouseEditParameter(int id)
            : base(id)
        {
        }

        public bool OpenOnLogisticsTab { get; init; }
    }
}