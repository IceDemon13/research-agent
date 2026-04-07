using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using Microsoft.Extensions.Logging;
using Telemart.Client.Common.Messages;
using Telemart.Client.Common.Services;
using Telemart.Client.Data.Requests.Features.Call.Actions;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.Mediator.Requests;
using Telemart.Client.Oktell;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.Call;
using Telemart.Client.ViewModels.Base;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.ViewModels.Dialogs.Call
{
    public sealed class CallDialogViewModel : TelemartViewModelBase, IDocumentContent
    {
        private int? callId;

        public CallDialogViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            ICallServiceClient callServiceClient,
            IMessenger messenger)
            : base(webClient, dictionaries, messageFacadeService)
        {
            CallServiceClient = callServiceClient;
            Messenger = messenger;

            EndCallCommand = new AsyncCommand(EndCallAsync, () => State == CallNotificationState.Start && CallServiceClient.CanEndCall());
            CancelCallCommand = new AsyncCommand(CancelCallAsync, () => State == CallNotificationState.Ring && CallServiceClient.CanCancelCall());
            ApplyCallCommand = new AsyncCommand(ApplyCallAsync, () => State == CallNotificationState.Ring && CallServiceClient.CanApplyCall());
            StartCallCommand = new AsyncCommand(StartCallAsync, () => State == CallNotificationState.End && CallServiceClient.CanCall());

            Messenger.Register<CallNotificationRequest>(this, OnCallNotificationRequest);

            State = CallNotificationState.End;
        }

        public IAsyncCommand EndCallCommand { get; }

        public IAsyncCommand CancelCallCommand { get; }

        public IAsyncCommand ApplyCallCommand { get; }

        public IAsyncCommand StartCallCommand { get; }

        public IDocumentOwner DocumentOwner { get; set; }

        public object Title
        {
            get { return GetProperty(() => Title); }
            private set { SetProperty(() => Title, value); }
        }

        public string Fio
        {
            get { return GetProperty(() => Fio); }
            set { SetProperty(() => Fio, value); }
        }

        public ReadOnlyObservableCollection<string> Phones
        {
            get { return GetProperty(() => Phones); }
            set { SetProperty(() => Phones, value); }
        }

        public string SelectedPhone
        {
            get { return GetProperty(() => SelectedPhone); }
            set { SetProperty(() => SelectedPhone, value); }
        }

        public string Ivr
        {
            get { return GetProperty(() => Ivr); }
            set { SetProperty(() => Ivr, value); }
        }

        public CallNotificationState State
        {
            get { return GetProperty(() => State); }
            set { SetProperty(() => State, value, StateChanged); }
        }

        public bool IsConnected => State is CallNotificationState.Start;

        public bool IsRinging => State is CallNotificationState.Ring;

        public bool EndCallVisible => State == CallNotificationState.Start && CallServiceClient.CanEndCall();

        public bool CancelCallVisible => State == CallNotificationState.Ring && CallServiceClient.CanCancelCall();

        public bool ApplyCallVisible => State == CallNotificationState.Ring && CallServiceClient.CanApplyCall();

        public bool StartCallVisible => State == CallNotificationState.End && CallServiceClient.CanCall();

        private ICallServiceClient CallServiceClient { get; }

        private IMessenger Messenger { get; }

        public async void OnClose(CancelEventArgs e)
        {
            if (IsConnected)
            {
                if (MessageFacadeService.Confirm("Вы уверены?"))
                {
                    await CallServiceClient.EndCallAsync().CatchAll(x => Logger.LogError(x, "Failed to end call"));
                }
                else
                {
                    e.Cancel = true;
                }
            }
        }

        public async Task SetParameterAsync(CallDialogParameter parameter)
        {
            callId = parameter.CallId;
            Fio = parameter.Fio;
            Ivr = parameter.Ivr;
            Phones = parameter.Phones.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).ToReadOnlyObservableCollection();

            SelectedPhone = Phones.FirstOrDefault();

            if (CallServiceClient.CanCall() && Phones.Count == 1 && parameter.AutoCall)
            {
                await StartCallAsync();
            }
        }

        public void OnDestroy()
        {
        }

        protected override Task HandleLoadedAsync()
        {
            Title = "Исходящий звонок";

            return Task.CompletedTask;
        }

        public void Clear()
        {
            callId = null;
            Fio = null;
            Phones = null;
            SelectedPhone = null;
        }

        private async Task<bool> StartCallAsync()
        {
            if (State != CallNotificationState.End)
            {
                return true;
            }

            OktellResult result = await CallServiceClient.CallAsync(SelectedPhone);

            if (result.IsError)
            {
                ShowError(result.Message);
            }
            else
            {
                if (callId.HasValue)
                {
                    Result<CallDto> makeCallResult = await WebClient.ExecuteApiRequestAsync(new MakeCall(callId.Value, SelectedPhone));

                    Messenger.Send(new CallMessage(makeCallResult.Data, MessageType.Changed));
                }
            }

            return !result.IsError;
        }

        private async Task EndCallAsync()
        {
            OktellResult result = await CallServiceClient.EndCallAsync();

            if (result.IsError)
            {
                ShowError(result.Message);
            }
        }

        private async Task CancelCallAsync()
        {
            OktellResult result = await CallServiceClient.CancelCallAsync();

            if (result.IsError)
            {
                ShowError(result.Message);
            }
        }

        private async Task ApplyCallAsync()
        {
            OktellResult result = await CallServiceClient.ApplyCallAsync();

            if (result.IsError)
            {
                ShowError(result.Message);
            }
        }

        private void CancelCall()
        {
            State = CallNotificationState.End;
            Messenger.Send(new CallDialogEndMessage());
        }

        private void StateChanged(CallNotificationState oldState)
        {
            if (State == CallNotificationState.End && oldState != CallNotificationState.End)
            {
                CancelCall();
            }

            RaisePropertiesChanged(
                nameof(IsConnected),
                nameof(IsRinging),
                nameof(EndCallVisible),
                nameof(StartCallVisible),
                nameof(ApplyCallVisible),
                nameof(CancelCallVisible));
        }

        private void ShowError(string error)
        {
            MessageFacadeService.ShowNotificationWarning(error);
            CancelCall();
        }

        private void OnCallNotificationRequest(CallNotificationRequest request)
        {
            State = request.State;
            Ivr = request.Ivr;
            SelectedPhone = request.Phone;
        }
    }
}