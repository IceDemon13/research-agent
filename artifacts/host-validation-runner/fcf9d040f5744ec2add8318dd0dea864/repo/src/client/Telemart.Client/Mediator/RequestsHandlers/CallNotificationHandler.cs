using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using AutoMapper;
using DevExpress.Mvvm;
using MediatR;
using Microsoft.Extensions.Logging;
using Telemart.Client.Common;
using Telemart.Client.Common.Messages;
using Telemart.Client.Data.Requests.Features.Call;
using Telemart.Client.Data.Requests.Features.Contractor;
using Telemart.Client.Data.Requests.Features.Customer;
using Telemart.Client.Data.Stores;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Dictionaries.Constants;
using Telemart.Client.Mediator.Requests;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.Call;
using Telemart.Client.TransferObjects.Customer;
using Telemart.Client.TransferObjects.Paging;
using Telemart.Client.ViewModels.Customer;
using Telemart.Client.ViewModels.Dialogs.Call;
using Telemart.Client.ViewModels.Store.Call;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.Mediator.RequestsHandlers
{
    public class CallNotificationHandler : INotificationHandler<CallNotificationRequest>
    {
        private readonly IMessenger _messenger;
        private readonly IWebClient _webClient;
        private readonly ICallStore _callStore;
        private readonly IMapper _mapper;
        private readonly ILogger<CallNotificationHandler> _logger;

        public CallNotificationHandler(IMessenger messenger, IWebClient webClient, ICallStore callStore, IMapper mapper, ILogger<CallNotificationHandler> logger)
        {
            _messenger = messenger;
            _webClient = webClient;
            _callStore = callStore;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task Handle(CallNotificationRequest notification, CancellationToken cancellationToken)
        {
            await Application.Current.Dispatcher.BeginInvoke(() => _messenger.Send(notification));

            switch (notification)
            {
                case { Type: CallNotificationType.Incoming, State: CallNotificationState.Ring }:
                    await HandleRingIncomingCallAsync(notification);
                    break;
                case { Type: CallNotificationType.Incoming, State: CallNotificationState.Start }:
                    await HandleStartIncomingCallAsync(notification);
                    break;
                case { Type: CallNotificationType.Outgoing, State: CallNotificationState.Ring }:
                    await HandleStartOutgoingCallAsync(notification);
                    break;
            }
        }

        private async Task HandleRingIncomingCallAsync(CallNotificationRequest notification)
        {
            await Application.Current.Dispatcher.BeginInvoke(
                () =>
                {
                    _messenger.Send(new CallDialogParameter(null, string.Empty, false, notification.Ivr, notification.Phone));
                });
        }

        private async Task HandleStartOutgoingCallAsync(CallNotificationRequest notification)
        {
            await Application.Current.Dispatcher.BeginInvoke(() => _messenger.Send(new PhoneHistoryViewMessage(notification.Phone)));
        }

        private async Task HandleStartIncomingCallAsync(CallNotificationRequest notification)
        {
            bool callCreated = false;

            try
            {
                CustomerFilteringItem item = new CustomerFilteringItem(notification.Phone);

                PagedResult<CustomerDto> customers = await _webClient.ExecuteApiRequestAsync(new QueryCustomers(item));

                CustomerDto customer = customers.Data.FirstOrDefault();

                int contractorId = customer?.ContractorId ?? Constants.TelemartContractorId;
                int subdivisionId;

                if (contractorId == Constants.TelemartContractorId)
                {
                    subdivisionId = Subdivision.Telemart.Id;
                }
                else
                {
                    ContractorDto contractor = await _webClient.ExecuteApiRequestAsync(new QueryContractor(contractorId));

                    subdivisionId = contractor.SubdivisionId;
                }

                Result<CallDto> createCallResult = await _webClient.ExecuteApiRequestAsync(
                    new CreateCall(new CallCreateDto()
                    {
                        Phone = notification.Phone,
                        ServiceRequestId = null,
                        ContractorId = contractorId,
                        PriorityId = Priority.Normal.Id,
                        SubdivisionId = subdivisionId,
                        OrderId = null,
                        Fio = customer?.Fio ?? "Клиент не найден",
                        ResponsibleEmployeeId = _webClient.AuthenticatedEmployee.Id,
                        CallFrom = DateTime.Now,
                        CallTo = DateTime.Now,
                        CallTypeId = CallTypeConstants.IncomingTypeId,
                        Incoming = true,
                        Task = string.IsNullOrWhiteSpace(notification.Ivr)
                            ? "Не указано"
                            : notification.Ivr
                    }));

                if (createCallResult.IsSuccess)
                {
                    callCreated = true;

                    _callStore.SetCallId(createCallResult.Data.Id);

                    await Application.Current.Dispatcher.BeginInvoke(() =>
                    {
                        _messenger.Send(new CallDialogParameter(
                            createCallResult.Data.Id,
                            customer?.Fio,
                            false,
                            notification.Ivr,
                            notification.Phone));

                        _messenger.Send(new OutcomingCallViewMessage(_mapper.Map<CallViewItem>(createCallResult.Data)));

                        _messenger.Send(new PhoneHistoryViewMessage(notification.Phone));
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create incoming call");
            }

            if (!callCreated)
            {
                await Application.Current.Dispatcher.BeginInvoke(() => _messenger.Send(new PhoneHistoryViewMessage(notification.Phone)));
            }
        }
    }
}