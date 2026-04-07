using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Setting
{
    public sealed class UpdateBusySecondsAfterCall : UpdateEntityRequestBase<object, UpdateBusySecondsAfterCallDto>
    {
        public UpdateBusySecondsAfterCall(short seconds)
            : base(new UpdateBusySecondsAfterCallDto { Seconds = seconds.ToString() }, ApiResources.Settings, "busy_seconds_after_call")
        {
        }
    }
}