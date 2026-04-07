using Telemart.Client.Data.Requests.Base.Action;

namespace Telemart.Client.Data.Requests.Features.PosTerminal
{
    public sealed class DeactivatePosSettings : CallEntityActionRequestResultBase<object>
    {
        public DeactivatePosSettings(int id)
            : base(id, $"{ApiResources.Pos}/settings", "disable")
        {
        }
    }
}