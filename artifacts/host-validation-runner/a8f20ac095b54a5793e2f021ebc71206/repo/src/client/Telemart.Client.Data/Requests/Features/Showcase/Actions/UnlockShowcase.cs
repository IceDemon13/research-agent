using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.Showcase;

namespace Telemart.Client.Data.Requests.Features.Showcase.Actions
{
    public class UnlockShowcase : UnlockRequestBase<ShowcaseDto>
    {
        public UnlockShowcase(int id, bool force = false)
            : base(force, ApiResources.Showcases, id)
        {
        }
    }
}
