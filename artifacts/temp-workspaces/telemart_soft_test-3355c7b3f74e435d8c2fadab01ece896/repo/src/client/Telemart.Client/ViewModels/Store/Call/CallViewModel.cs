using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using Telemart.Client.Business;
using Telemart.Client.Common;
using Telemart.Client.Common.Messages;
using Telemart.Client.Common.Services;
using Telemart.Client.Data.Requests.Features.Call;
using Telemart.Client.Data.Requests.Features.Call.Actions;
using Telemart.Client.Data.Requests.Features.Contractor;
using Telemart.Client.Data.Requests.Features.Employee;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.Call;
using Telemart.Client.ViewModels.Base;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.ViewModels.Store.Call
{
    public sealed class CallViewModel : TelemartEditorViewModelBase<CallDto, CallViewMessage, CallViewItem>
    {
        public CallViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            IMapper mapper,
            IMessenger messenger,
            DocumentCommands documentCommands)
            : base(webClient, dictionaries, messageFacadeService, mapper, messenger)
        {
            DocumentCommands = documentCommands;

            OpenOrderCommand = new DelegateCommand<int>(OpenOrder);
            OpenServiceRequestCommand = new DelegateCommand<int>(OpenServiceRequest);
        }

        public CallViewModel()
        {
        }

        #region Commands

        public IDelegateCommand OpenOrderCommand { get; }

        public IDelegateCommand OpenServiceRequestCommand { get; }

        public DocumentCommands DocumentCommands { get; }

        #endregion Commands

        #region INPC

        public ReadOnlyObservableCollection<ContractorDto> AllContractors
        {
            get { return GetProperty(() => AllContractors); }
            private set { SetProperty(() => AllContractors, value); }
        }

        public ReadOnlyObservableCollection<EmployeeDto> AllEmployees
        {
            get { return GetProperty(() => AllEmployees); }
            private set { SetProperty(() => AllEmployees, value); }
        }

        public ReadOnlyObservableCollection<CallTypeDto> CallTypes
        {
            get { return GetProperty(() => CallTypes); }
            private set { SetProperty(() => CallTypes, value); }
        }

        public CallTypeDto CurrentCallType
        {
            get { return GetProperty(() => CurrentCallType); }
            set { SetProperty(() => CurrentCallType, value, () => Model.CallTypeId = value.Id); }
        }

        public ReadOnlyObservableCollection<EmployeeDto> Employees
        {
            get { return GetProperty(() => Employees); }
            private set { SetProperty(() => Employees, value); }
        }

        public ReadOnlyObservableCollection<Priority> Priorities
        {
            get { return GetProperty(() => Priorities); }
            private set { SetProperty(() => Priorities, value); }
        }

        public IEnumerable<SummaryViewItem> SummaryItems
        {
            get { return GetProperty(() => SummaryItems); }
            private set { SetProperty(() => SummaryItems, value); }
        }

        #endregion INPC

        protected override string CreatedActionMessage { get; } = "создан";

        protected override string EntityName { get; } = "Звонок";

        protected override string UpdatedActionMessage { get; } = "сохранен";

        protected override Task<Result<CallDto>> CreateEntityAsync()
        {
            throw new NotSupportedException();
        }

        protected override object CreateEntityMessage(CallDto dto, MessageType messageType)
        {
            return new CallMessage(dto, messageType);
        }

        protected override Task<CallDto> GetEntityAsync(int id)
        {
            return WebClient.ExecuteApiRequestAsync(new QueryCall(id));
        }

        protected override IEnumerable<string> GetMembersToIgnore()
        {
            yield return nameof(Model.CallDisplayTime);
            yield return nameof(Model.DisplayPhone);
            yield return nameof(Model.ExpirationTime);
            yield return nameof(Model.Subdivision);
            yield return nameof(Model.EmployeeLockId);
            yield return nameof(Model.EmployeeLockName);
        }

        protected override async Task HandleLoadedAsync()
        {
            CallViewMessage message = (CallViewMessage)Parameter;

            if (message.IsNew)
            {
                throw new NotSupportedException("Call creation is not supported");
            }

            Priorities = Dictionaries.GetItems<Priority>().ToReadOnlyObservableCollection();

            List<CallTypeDto> callTypes = await WebClient.ExecuteApiRequestAsync(new QueryCallTypes(), true).GetPagedResultDataAsync();
            List<EmployeeDto> employees = await WebClient.ExecuteApiRequestAsync(new QueryEmployees(), true).GetPagedResultDataAsync();
            List<ContractorDto> contractors = await WebClient.ExecuteApiRequestAsync(new QueryContractors(), true).GetPagedResultDataAsync();

            CallTypes = callTypes.OrderBy(x => x.Position).ToReadOnlyObservableCollection();
            AllEmployees = employees.ToReadOnlyObservableCollection();
            Employees = employees
                .Where(x => x.Active && x.HasAnyRole(Role.Operator, Role.Manager, Role.ServiceManager, Role.Seller))
                .OrderBy(x => x.Name)
                .ToReadOnlyObservableCollection();
            AllContractors = contractors.ToReadOnlyObservableCollection();

            await base.HandleLoadedAsync();

            CurrentCallType = CallTypes.FirstOrDefault(x => x.Id == Model.CallTypeId);

            RefreshSummaryItems();
        }

        protected override Task<LockResponse<CallDto>> LockEntityAsync(int id)
        {
            return WebClient.ExecuteApiRequestAsync(new LockCall(id));
        }

        protected override Task<LockResponse<CallDto>> UnlockEntityAsync(int id)
        {
            return WebClient.ExecuteApiRequestAsync(new UnlockCall(id));
        }

        protected override void OnInitializeInDesignMode()
        {
            base.OnInitializeInDesignMode();

            SummaryItems = new[]
            {
                new SummaryViewItem("Подразд.", "Телемарт"),
                new SummaryViewItem("Контрагент", "Телемарт"),
                new SummaryViewItem("Создал", "Какой-то юзер"),
                new SummaryViewItem("Завершил", "-"),
                new SummaryViewItem("Завершен", "-"),
                new SummaryViewItem("Результат", "-"),
                new SummaryViewItem("Попытка", "1")
            };
        }

        protected override async Task<bool> SaveAsync()
        {
            if (CurrentCallType.ParentId == null)
            {
                MessageFacadeService.ShowNotificationWarning("Выберите тип, а не группу");
                return false;
            }

            if (Model.CallFrom > Model.CallTo)
            {
                MessageFacadeService.ShowNotificationWarning("Дата начала звонка должна быть меньше, чем дата окончания");
                return false;
            }

            bool processed = await base.SaveAsync();

            return processed;
        }

        protected override void SetCreateTitle()
        {
            Title = "Создание звонка";
        }

        protected override void SetEditTitle()
        {
            Title = $"Звонок №{Model.Id}";
        }

        protected override Task<Result<CallDto>> UpdateEntityAsync()
        {
            CallSaveDto saveDto = Mapper.Map<CallSaveDto>(Model);
            UpdateCall gatewayRequest = new UpdateCall(saveDto);
            return WebClient.ExecuteApiRequestAsync(gatewayRequest);
        }

        private IEnumerable<SummaryViewItem> GetSummaryItems()
        {
            string createdByAndWhen = $"{AllEmployees.FirstOrDefault(x => x.Id == Model.EmployeeCreatedById)?.Name} ({Model.CreatedOn.ToString(DateFormattingRules.FullDateTimeFormat)})";

            yield return new SummaryViewItem("Подразд.", Model.Subdivision.Name);

            if (Model.ContractorId.HasValue)
            {
                yield return new SummaryViewItem("Контрагент", AllContractors.FirstOrDefault(x => x.Id == Model.ContractorId)?.Name);
            }

            yield return new SummaryViewItem("Создал", createdByAndWhen);

            if (Model.EmployeeCompletedById.HasValue)
            {
                yield return new SummaryViewItem("Завершил", AllEmployees.FirstOrDefault(x => x.Id == Model.EmployeeCompletedById)?.Name);
            }

            if (Model.CompletedOn.HasValue)
            {
                yield return new SummaryViewItem("Завершен", Model.CompletedOn.Value.ToString(DateFormattingRules.FullDateTimeFormat));
            }

            if (!string.IsNullOrEmpty(Model.Result))
            {
                yield return new SummaryViewItem("Результат", Model.Result);
            }

            yield return new SummaryViewItem("Статус", Model.CallState.Name);

            yield return new SummaryViewItem("Попытка", Model.Attempt.ToString());
        }

        private void OpenOrder(int orderId)
        {
            Messenger.Send(new OrderEditViewMessage(orderId));
        }

        private void OpenServiceRequest(int serviceRequestId)
        {
            Messenger.Send(new ServiceRequestViewMessage(serviceRequestId));
        }

        private void RefreshSummaryItems()
        {
            SummaryItems = GetSummaryItems();
        }
    }
}