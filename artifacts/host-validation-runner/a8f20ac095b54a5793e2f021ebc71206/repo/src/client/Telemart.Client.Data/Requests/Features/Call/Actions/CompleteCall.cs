using System;
using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects.Call;

namespace Telemart.Client.Data.Requests.Features.Call.Actions
{
    public sealed class CompleteCall : CallEntityActionWithBodyRequestResultBase<CallDto, OutcomingCallDto>
    {
        public CompleteCall(int callId, int state, string result, string task, bool callLater, DateTime? callFrom, DateTime? callTo, CallDependencyDto[] dependencies)
            : base(
                callId,
                new OutcomingCallDto
                {
                    Id = callId,
                    State = state,
                    Result = result,
                    Task = task,
                    Dependencies = dependencies,
                    CallLater = callLater,
                    CallFrom = callFrom,
                    CallTo = callTo
                },
                ApiResources.Calls,
                "complete")
        {
        }
    }
}