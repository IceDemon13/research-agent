using System;
using System.Threading.Tasks;
using MediatR;
using Quartz;
using Telemart.Client.Mediator.Requests;
using Telemart.Client.Oktell;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Jobs
{
    [DisallowConcurrentExecution]
    [PersistJobDataAfterExecution]
    public class ShowPhoneHistoryJob : IJob
    {
        public const string ConnectedStateIdentity = nameof(ConnectedStateIdentity);

        private readonly IPublisher _publisher;
        private readonly ICallServiceClient _callServiceClient;

        public ShowPhoneHistoryJob(
            ICallServiceClient callServiceClient,
            IPublisher publisher)
        {
            _callServiceClient = callServiceClient ?? throw new ArgumentNullException(nameof(callServiceClient));
            _publisher = publisher ?? throw new ArgumentNullException(nameof(publisher));
        }

        public async Task Execute(IJobExecutionContext context)
        {
            CallNotificationRequest state = context.JobDetail.JobDataMap.Get(ConnectedStateIdentity) as CallNotificationRequest;

            OktellStateResult result = await _callServiceClient.GetStateAsync();

            if (result.IsOk)
            {
                CallNotificationRequest currentState = new CallNotificationRequest
                {
                    State = GetState(result),
                    Type = GetType(result),
                    Ivr = result.Name,
                    Phone = result.Phone,
                };

                if (!currentState.Equals(state))
                {
                    await _publisher.Publish(currentState);

                    context.JobDetail.JobDataMap[ConnectedStateIdentity] = currentState;
                }
            }
        }

        private CallNotificationState GetState(OktellStateResult result)
        {
            return result.State switch
            {
                OktellState.Connected => CallNotificationState.Start,
                OktellState.Ringing => CallNotificationState.Ring,
                _ => CallNotificationState.End
            };
        }

        private CallNotificationType GetType(OktellStateResult result)
        {
            return result.Type is OktellType.Inner or OktellType.Ivr or OktellType.Task
                    ? CallNotificationType.Incoming
                    : CallNotificationType.Outgoing;
        }
    }
}