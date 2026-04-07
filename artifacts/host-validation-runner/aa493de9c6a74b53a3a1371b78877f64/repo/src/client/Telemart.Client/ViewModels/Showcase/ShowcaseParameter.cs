using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.Showcase
{
    public class ShowcaseParameter : EditorParameter
    {
        public ShowcaseParameter(int id, int? productId = null)
            : base(id)
        {
            ProductId = productId;
        }

        public int? ProductId { get; }
    }
}
