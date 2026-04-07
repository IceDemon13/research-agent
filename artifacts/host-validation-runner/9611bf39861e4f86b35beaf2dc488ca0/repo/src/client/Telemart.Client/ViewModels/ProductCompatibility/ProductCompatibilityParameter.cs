using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.ProductCompatibility
{
    public class ProductCompatibilityParameter : EditorParameter
    {
        public ProductCompatibilityParameter(int id, int? typeId = null)
            : base(id)
        {
            TypeId = typeId;
        }

        public int? TypeId { get; }
    }
}
