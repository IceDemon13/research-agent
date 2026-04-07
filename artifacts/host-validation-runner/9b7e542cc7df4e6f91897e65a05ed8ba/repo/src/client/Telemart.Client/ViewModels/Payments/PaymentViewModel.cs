using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using AutoMapper;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using Telemart.Client.Common.Messages;
using Telemart.Client.Common.Services;
using Telemart.Client.Data.Requests.Features.Payments;
using Telemart.Client.Data.Requests.Features.Payments.Actions;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.Payments;
using Telemart.Client.ViewModels.Base;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.ViewModels.Payments
{
    public class PaymentViewModel : TelemartEditorViewModelBase<PaymentDto, PaymentParameter, PaymentViewItem>
    {
        public PaymentViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            IMessenger messenger,
            IMapper mapper)
            : base(webClient, dictionaries, messageFacadeService, mapper, messenger)
        {
        }

        public PaymentViewModel()
        {
        }

        public ReadOnlyCollection<EntityActiveValue> ActiveValues
        {
            get { return GetProperty(() => ActiveValues); }
            private set { SetProperty(() => ActiveValues, value); }
        }

        protected override string CreatedActionMessage => throw new NotSupportedException();

        protected override string EntityName { get; } = "Способ оплаты";

        protected override string UpdatedActionMessage { get; } = "сохранен";

        protected override Task<Result<PaymentDto>> CreateEntityAsync()
        {
            throw new NotSupportedException();
        }

        protected override object CreateEntityMessage(PaymentDto dto, MessageType messageType)
        {
            return new PaymentMessage(dto, messageType);
        }

        protected override Task<PaymentDto> GetEntityAsync(int id)
        {
            return WebClient.ExecuteApiRequestAsync(new QueryPayment(id));
        }

        protected override Task HandleLoadedAsync()
        {
            ActiveValues = Dictionaries.GetItems<EntityActiveValue>().ToReadOnlyObservableCollection();

            return base.HandleLoadedAsync();
        }

        protected override Task<LockResponse<PaymentDto>> LockEntityAsync(int id)
        {
            return WebClient.ExecuteApiRequestAsync(new LockPayment(id));
        }

        protected override Task<LockResponse<PaymentDto>> UnlockEntityAsync(int id)
        {
            return WebClient.ExecuteApiRequestAsync(new UnlockPayment(id));
        }

        protected override void SetEditTitle()
        {
            Title = $"{Model.Name} ({Model.Id})";
        }

        protected override Task<Result<PaymentDto>> UpdateEntityAsync()
        {
            PaymentSaveDto saveDto = new PaymentSaveDto(Model.Id, Model.Name, Model.NameUa, Model.NameEn, Model.Fee, Model.ProviderFee, Model.LimitUah, Model.LimitUsd, Model.Active);

            return WebClient.ExecuteApiRequestAsync(new UpdatePayment(saveDto));
        }

        protected override void SetCreateTitle()
        {
            throw new NotSupportedException();
        }
    }
}