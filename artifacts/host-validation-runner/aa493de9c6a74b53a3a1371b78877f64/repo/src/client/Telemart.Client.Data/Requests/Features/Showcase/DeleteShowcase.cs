using Telemart.Client.Data.Requests.Base;

namespace Telemart.Client.Data.Requests.Features.Showcase
{
    public class DeleteShowcase : DeleteEntityResultRequestBase<object>
    {
        public DeleteShowcase(int id)
            : base(ApiResources.Showcases, id)
        {
        }
    }
}