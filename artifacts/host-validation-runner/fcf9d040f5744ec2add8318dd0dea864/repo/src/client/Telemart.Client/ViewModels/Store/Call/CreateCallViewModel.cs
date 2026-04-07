using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using DevExpress.Mvvm;
using DevExpress.Mvvm.DataAnnotations;
using DevExpress.Mvvm.Native;
using Microsoft.Extensions.Logging;
using Telemart.Client.Common.Messages;
using Telemart.Client.Common.Services;
using Telemart.Client.Core.Exceptions;
using Telemart.Client.Data.Requests.Features.Call;
using Telemart.Client.Data.Requests.Features.Call.Actions;
using Telemart.Client.Data.Requests.Features.Contractor;
using Telemart.Client.Data.Requests.Features.Contractor.Contact;
using Telemart.Client.Data.Requests.Features.Employee;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.Properties;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.Call;
using Telemart.Client.ViewModels.Base;
using Telemart.Client.ViewModels.Validation;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.ViewModels.Store.Call
{
    public sealed class CreateCallViewModel : TelemartDialogViewModelBase
    {
        private int? orderId;
        private int? serviceRequestId;
        private int defaultParentCallTypeId;
        private List<ContractorDto> contractors;

        public CreateCallViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            IMessenger messenger)
            : base(webClient, dictionaries, messageFacadeService)
        {
            Messenger = messenger;

            RefreshContractorContactsCommand = new AsyncCommand<int>(RefreshContractorContactsAsync);
            HandleCallTypeChangedCommand = new AsyncCommand(HandleCallTypeChangedAsync);
        }

        public CreateCallViewModel()
        {
        }

        #region Commands

        public IAsyncCommand RefreshContractorContactsCommand { get; }

        public IAsyncCommand HandleCallTypeChangedCommand { get; }

        #endregion Commands

        #region INPC

        public DateTime? CallFrom
        {
            get { return GetProperty(() => CallFrom); }
            set { SetProperty(() => CallFrom, value); }
        }

        public DateTime? CallTo
        {
            get { return GetProperty(() => CallTo); }
            set { SetProperty(() => CallTo, value); }
        }

        public ReadOnlyObservableCollection<CallTypeDto> CallTypes
        {
            get { return GetProperty(() => CallTypes); }
            private set { SetProperty(() => CallTypes, value); }
        }

        public CallTypeDto CurrentCallType
        {
            get { return GetProperty(() => CurrentCallType); }
            set { SetProperty(() => CurrentCallType, value); }
        }

        public ReadOnlyObservableCollection<EmployeeDto> Employees
        {
            get { return GetProperty(() => Employees); }
            private set { SetProperty(() => Employees, value); }
        }

        public ObservableCollection<ContractorDto> Contractors
        {
            get { return GetProperty(() => Contractors); }
            private set { SetProperty(() => Contractors, value); }
        }

        public ReadOnlyObservableCollection<ContractorContactDto> ContractorContacts
        {
            get { return GetProperty(() => ContractorContacts); }
            private set { SetProperty(() => ContractorContacts, value); }
        }

        public string Fio
        {
            get { return GetProperty(() => Fio); }
            set { SetProperty(() => Fio, value); }
        }

        public string Phone
        {
            get { return GetProperty(() => Phone); }
            set { SetProperty(() => Phone, value); }
        }

        public string Phone2
        {
            get { return GetProperty(() => Phone2); }
            set { SetProperty(() => Phone2, value); }
        }

        public ReadOnlyObservableCollection<Priority> Priorities
        {
            get { return GetProperty(() => Priorities); }
            private set { SetProperty(() => Priorities, value); }
        }

        public int? ResponsibleEmployeeId
        {
            get { return GetProperty(() => ResponsibleEmployeeId); }
            set { SetProperty(() => ResponsibleEmployeeId, value); }
        }

        public Priority SelectedPriority
        {
            get { return GetProperty(() => SelectedPriority); }
            set { SetProperty(() => SelectedPriority, value); }
        }

        public Subdivision SelectedSubdivision
        {
            get { return GetProperty(() => SelectedSubdivision); }
            set { SetProperty(() => SelectedSubdivision, value, OnSubdivisionChanged); }
        }

        public ContractorDto SelectedContractor
        {
            get { return GetProperty(() => SelectedContractor); }
            set { SetProperty(() => SelectedContractor, value, OnContractorChanged); }
        }

        public ContractorContactDto SelectedContractorContact
        {
            get { return GetProperty(() => SelectedContractorContact); }
            set { SetProperty(() => SelectedContractorContact, value, OnContractorContactChanged); }
        }

        public ReadOnlyObservableCollection<Subdivision> Subdivisions
        {
            get { return GetProperty(() => Subdivisions); }
            private set { SetProperty(() => Subdivisions, value); }
        }

        public string Task
        {
            get { return GetProperty(() => Task); }
            set { SetProperty(() => Task, value); }
        }

        public bool AllowToEditSubdivision
        {
            get { return GetProperty(() => AllowToEditSubdivision); }
            private set { SetProperty(() => AllowToEditSubdivision, value); }
        }

        public bool AllowToEditContractor
        {
            get { return GetProperty(() => AllowToEditContractor); }
            set { SetProperty(() => AllowToEditContractor, value); }
        }

        public bool AllowToEditContractorContact => SelectedSubdivision?.IsRetail == false;

        #endregion INPC

        public CallDto ResultCall { get; private set; }

        private IMessenger Messenger { get; }

        public static void BuildMetadata(MetadataBuilder<CreateCallViewModel> builder)
        {
            builder.Property(x => x.SelectedSubdivision).Required(() => Resources.RequiredErrorMessage);
            builder.Property(x => x.SelectedContractor).Required(() => Resources.RequiredErrorMessage);
            builder.Property(x => x.SelectedContractorContact).MatchesInstanceRule((x, y) => !y.AllowToEditContractorContact || x != null, () => Resources.RequiredErrorMessage);
            builder.Property(x => x.CurrentCallType).Required(() => Resources.RequiredErrorMessage);
            builder.Property(x => x.Fio).Required(() => Resources.RequiredErrorMessage);
            builder.Property(x => x.Phone).Required(() => Resources.RequiredErrorMessage);
            builder.Property(x => x.CallFrom).Required(() => Resources.RequiredErrorMessage);
            builder.Property(x => x.CallTo).Required(() => Resources.RequiredErrorMessage);
            builder.Property(x => x.Task).MatchesRule(x => !string.IsNullOrWhiteSpace(x), () => Resources.RequiredErrorMessage);
        }

        protected override async Task HandleLoadedAsync()
        {
            CreateCallParameter parameter = (CreateCallParameter)Parameter;

            List<CallTypeDto> callTypes = await WebClient.ExecuteApiRequestAsync(new QueryCallTypes(), true).GetPagedResultDataAsync();
            List<EmployeeDto> employees = await WebClient.ExecuteApiRequestAsync(new QueryEmployees(), true).GetPagedResultDataAsync();
            contractors = await WebClient.ExecuteApiRequestAsync(new QueryContractors(), true).GetPagedResultDataAsync();

            Subdivisions = Dictionaries
                .GetItems<Subdivision>()
                .Where(x => WebClient.AuthenticatedEmployee.AllowSubdivisions.Contains(x.Id))
                .ToReadOnlyObservableCollection();

            Priorities = Dictionaries
                .GetItems<Priority>()
                .ToReadOnlyObservableCollection();

            Employees = employees.Where(x => x.Active && x.HasAnyRole(Role.Operator, Role.Manager))
                .OrderBy(x => x.Name)
                .ToReadOnlyObservableCollection();

            RefreshContractors(SelectedSubdivision?.Id ?? 0);

            defaultParentCallTypeId = parameter.DocumentType != CallDocumentType.None ?
                (int)parameter.DocumentType
                : 0;

            CallTypes = callTypes
                .OrderBy(x => x.Position)
                .ToReadOnlyObservableCollection();

            orderId = parameter.OrderId;
            serviceRequestId = parameter.ServiceRquestId;

            Fio = parameter.Fio;
            Phone = parameter.Phone;
            Phone2 = parameter.Phone2;

            if (parameter.SubdivisionId.HasValue)
            {
                SelectedSubdivision = Subdivisions.FirstOrDefault(x => x.Id == parameter.SubdivisionId.Value);
            }

            AllowToEditSubdivision = SelectedSubdivision == null;

            if (AllowToEditSubdivision)
            {
                SelectedSubdivision = Dictionaries.GetItemById<Subdivision>(WebClient.AuthenticatedEmployee.SubdivisionId);
            }

            AllowToEditContractor = !parameter.ContractorId.HasValue;

            if (parameter.ContractorId.HasValue)
            {
                ContractorDto contractor = contractors.First(x => x.Id == parameter.ContractorId.Value);

                if (Contractors.All(x => x.Id != contractor.Id))
                {
                    Contractors.Add(contractor);
                }

                SelectedContractor = contractor;
            }

            SelectedPriority = parameter.Priority;

            await HandleCallTypeChangedAsync();

            Title = "Создание звонка";
        }

        protected override async Task HandleOkAsync()
        {
            if (CurrentCallType == null)
            {
                MessageFacadeService.ShowNotificationWarning("Выберите тип звонка");
                return;
            }

            if (IDataErrorInfoHelper.HasErrors(this))
            {
                return;
            }

            if (CurrentCallType.ParentId == null)
            {
                MessageFacadeService.ShowNotificationWarning("Выберите тип, а не группу");
                return;
            }

            if (CallFrom > CallTo)
            {
                MessageFacadeService.ShowNotificationWarning("Дата начала звонка должна быть меньше, чем дата окончания");
                return;
            }

            CallCreateDto createDto = MapToDto(this, new CallCreateDto());

            try
            {
                Result<CallDto> result = await WebClient.ExecuteApiRequestAsync(new CreateCall(createDto));

                ResultCall = result.Data;

                Messenger.Send(new CallMessage(result.Data, MessageType.Added));

                IsOk = true;
                Close();
            }
            catch (UnexpectedSatusException exception)
            {
                MessageFacadeService.ShowNotificationError("Ошибка при создании звонка");
                ShowValidationResultView("Ошибки при создании звонка", exception.GetErrorItems());
            }
            catch (UnexpectedErrorException exception)
            {
                Logger.LogError(exception, "Failed to create Call");
                ShowValidationResultView(Resources.ServerConnectError, new[] { new ValidationResultItem(Resources.ServerUnavailable, true) });
            }
            catch (Exception exception)
            {
                MessageFacadeService.ShowNotificationError("Ошибка при создании звонка");
                Logger.LogError(exception, "Error while creating call");
            }
        }

        private static CallCreateDto MapToDto(CreateCallViewModel source, CallCreateDto target)
        {
            target.OrderId = source.orderId;
            target.ServiceRequestId = source.serviceRequestId;
            target.CallFrom = source.CallFrom.Value;
            target.CallTo = source.CallTo.Value;
            target.CallTypeId = source.CurrentCallType.Id;
            target.Fio = source.Fio;
            target.Phone = source.Phone;
            target.Phone2 = source.Phone2;
            target.PriorityId = source.SelectedPriority.Id;
            target.ResponsibleEmployeeId = source.ResponsibleEmployeeId;
            target.SubdivisionId = source.SelectedSubdivision.Id;
            target.ContractorId = source.SelectedContractor?.Id;
            target.Task = source.Task;

            return target;
        }

        private void OnSubdivisionChanged()
        {
            RefreshContractors(SelectedSubdivision?.Id ?? 0);
            RaisePropertiesChanged(nameof(AllowToEditContractorContact), nameof(SelectedContractorContact));
        }

        private void OnContractorChanged()
        {
            if (SelectedContractor == null)
            {
                ContractorContacts = null;
                return;
            }

            RefreshContractorContactsCommand.Execute(SelectedContractor.Id);
            RaisePropertiesChanged(nameof(AllowToEditContractorContact), nameof(SelectedContractorContact));
        }

        private void OnContractorContactChanged()
        {
            Fio = SelectedContractorContact?.FullName;
            Phone = SelectedContractorContact?.Phone1;
        }

        private async Task HandleCallTypeChangedAsync()
        {
            try
            {
                QueryCallInterval request = new QueryCallInterval(CurrentCallType?.ParentId ?? defaultParentCallTypeId, 1);

                CallIntervalResponse response = await WebClient.ExecuteApiRequestAsync(request);

                CallFrom = response.Form;
                CallTo = response.To;
            }
            catch (Exception exception)
            {
                CallFrom = null;
                CallTo = null;

                MessageFacadeService.ShowNotificationError("Ошибка при вычислении интервала");
                Logger.LogError(exception, "Error calculating interval");
            }
        }

        private void RefreshContractors(int? subdivisionId)
        {
            Contractors = contractors
                .Where(x => x.Active && !x.IsFolder && x.IsClient && (subdivisionId == null || x.SubdivisionId == subdivisionId.Value))
                .OrderBy(x => x.Name)
                .ToObservableCollection();
        }

        private async Task RefreshContractorContactsAsync(int contractorId)
        {
            SelectedContractorContact = null;

            if (!AllowToEditContractorContact)
            {
                return;
            }

            try
            {
                IReadOnlyCollection<ContractorContactDto> result = await WebClient.ExecuteApiRequestAsync(new QueryContractorContacts(contractorId));

                ContractorContacts = result.Where(x => x.Active).ToReadOnlyObservableCollection();
            }
            catch (Exception exception)
            {
                MessageFacadeService.ShowNotificationError("Ошибка при обновлении контактов");
                Logger.LogError(exception, "Error while refreshing contractor contacts. ({ContractorId})", contractorId);
            }
        }
    }
}