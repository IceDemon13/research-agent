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
using Telemart.Client.Common.MvvmEnhancements;
using Telemart.Client.Common.Services;
using Telemart.Client.Data.Requests.Features.Complaint;
using Telemart.Client.Data.Requests.Features.Complaint.Actions;
using Telemart.Client.Data.Requests.Features.Contractor;
using Telemart.Client.Data.Requests.Features.Employee;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.Complaint;
using Telemart.Client.ViewModels.Base;
using Telemart.Client.ViewModels.Dialogs;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.ViewModels.Complaint
{
    public class ComplaintViewModel : TelemartEditorViewModelBase<ComplaintDto, ComplaintParameter, ComplaintViewItem>
    {
        private IReadOnlyDictionary<int, string> contractors;
        private IReadOnlyDictionary<int, string> employees;
        private IReadOnlyDictionary<int, ComplaintTypeDto> types;
        private IReadOnlyDictionary<int, string> sources;

        public ComplaintViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            IMapper mapper,
            IMessenger messenger,
            DocumentCommands documentCommands)
            : base(webClient, dictionaries, messageFacadeService, mapper, messenger)
        {
            DocumentCommands = documentCommands;

            SetStateCommand = new AsyncCommand<ComplaintState>(SetStateAsync, x => x?.IsFinished == true);
        }

        public IAsyncCommand SetStateCommand { get; }

        public DocumentCommands DocumentCommands { get; }

        #region INPC

        public ReadOnlyObservableCollection<ComboBoxItem> Employees
        {
            get { return GetProperty(() => Employees); }
            private set { SetProperty(() => Employees, value); }
        }

        public ReadOnlyObservableCollection<Priority> Priorities
        {
            get { return GetProperty(() => Priorities); }
            private set { SetProperty(() => Priorities, value); }
        }

        public ReadOnlyObservableCollection<ComplaintState> FinishedStates
        {
            get { return GetProperty(() => FinishedStates); }
            private set { SetProperty(() => FinishedStates, value); }
        }

        public IEnumerable<SummaryViewItem> SummaryItems
        {
            get { return GetProperty(() => SummaryItems); }
            private set { SetProperty(() => SummaryItems, value); }
        }

        #endregion

        public bool ButtonsIsVisible => Model != null && Model.EmployeeLockId == null && !Model.State.IsFinished;

        protected override string CreatedActionMessage => throw new NotSupportedException();

        protected override string EntityName { get; } = "Жалоба";

        protected override string UpdatedActionMessage { get; } = "сохранена";

        protected override async Task HandleLoadedAsync()
        {
            Priorities = Dictionaries.GetItems<Priority>().ToReadOnlyObservableCollection();
            FinishedStates = Dictionaries.GetItems<ComplaintState>().Where(x => x.IsFinished).OrderBy(x => x.Position).ToReadOnlyObservableCollection();

            await Task.WhenAll(RefreshContractors(), RefreshEmployees(), RefreshTypes(), RefreshSourses());

            await base.HandleLoadedAsync();

            async Task RefreshContractors()
            {
                List<ContractorDto> contractorDtos = await WebClient.ExecuteApiRequestAsync(new QueryContractors(), true).GetPagedResultDataAsync();

                contractors = contractorDtos.ToDictionary(x => x.Id, y => y.Name);
            }

            async Task RefreshEmployees()
            {
                List<EmployeeDto> employeeDtos = await WebClient.ExecuteApiRequestAsync(new QueryEmployees(), true).GetPagedResultDataAsync();

                employees = employeeDtos.ToDictionary(x => x.Id, y => y.Name);

                Employees = employeeDtos
                    .Where(x => x.Active)
                    .Select(x => new ComboBoxItem(x.Id, x.Name))
                    .OrderBy(x => x.DisplayValue)
                    .ToReadOnlyObservableCollection();
            }

            async Task RefreshTypes()
            {
                List<ComplaintTypeDto> typeDtos = await WebClient.ExecuteApiRequestAsync(new QueryComplaintTypes());

                types = typeDtos.ToDictionary(x => x.Id);
            }

            async Task RefreshSourses()
            {
                List<ComplaintSourceDto> sourcesDtos = await WebClient.ExecuteApiRequestAsync(new QueryComplaintSources());

                sources = sourcesDtos.OrderBy(x => x.Name).ToDictionary(x => x.Id, y => y.Name);
            }
        }

        protected override Task<Result<ComplaintDto>> CreateEntityAsync() => throw new NotSupportedException();

        protected override void SetCreateTitle() => throw new NotSupportedException();

        protected override object CreateEntityMessage(ComplaintDto dto, MessageType messageType)
        {
            return new ComplaintMessage(dto, messageType);
        }

        protected override void AfterSetData()
        {
            SummaryItems = GetSummaryItems();
            RaisePropertyChanged(nameof(ButtonsIsVisible));
        }

        protected override Task<ComplaintDto> GetEntityAsync(int id)
        {
            return WebClient.ExecuteApiRequestAsync(new QueryComplaint(id));
        }

        protected override Task<LockResponse<ComplaintDto>> LockEntityAsync(int id)
        {
            return WebClient.ExecuteApiRequestAsync(new LockComplaint(id));
        }

        protected override Task<LockResponse<ComplaintDto>> UnlockEntityAsync(int id)
        {
            return WebClient.ExecuteApiRequestAsync(new UnlockComplaint(id));
        }

        protected override void SetEditTitle()
        {
            Title = $"Жалоба №{Model.Id}";
        }

        protected override Task<Result<ComplaintDto>> UpdateEntityAsync()
        {
            return WebClient.ExecuteApiRequestAsync(new UpdateComplaint(Model.Id, Mapper.Map<ComplaintSaveDto>(Model)));
        }

        private IEnumerable<SummaryViewItem> GetSummaryItems()
        {
            yield return new SummaryViewItem("Номер", Model.Id.ToString());
            yield return new SummaryViewItem("Создал", $"{employees.GetValueOrDefault(Model.CreatedBy)} ({Model.CreatedOn.ToString(DateFormattingRules.FullDateTimeFormat)})");
            yield return new SummaryViewItem("Изменил", $"{employees.GetValueOrDefault(Model.ModifiedBy)} ({Model.ModifiedOn.ToString(DateFormattingRules.FullDateTimeFormat)})");

            if (Model.CompletedBy.HasValue)
            {
                yield return new SummaryViewItem("Завершил", $"{employees.GetValueOrDefault(Model.CompletedBy.Value)} ({Model.CompletedOn.Value.ToString(DateFormattingRules.FullDateTimeFormat)})");
            }

            if (Model.ContractorId.HasValue)
            {
                yield return new SummaryViewItem("Контрагент", contractors.GetValueOrDefault(Model.ContractorId.Value));
            }

            if (types.TryGetValue(Model.TypeId, out ComplaintTypeDto subType) && subType.ParentId.HasValue
                && types.TryGetValue(subType.ParentId.Value, out ComplaintTypeDto type))
            {
                yield return new SummaryViewItem("Тип", $"{type.Name} ({subType.Name})");
            }

            yield return new SummaryViewItem("Источник", sources.GetValueOrDefault(Model.SourceId));
            yield return new SummaryViewItem("Статус", $"{Model.State.Name}{(string.IsNullOrWhiteSpace(Model.Resolution) ? string.Empty : $" ({Model.Resolution.Trim()})")}");
        }

        private Task SetStateAsync(ComplaintState state)
        {
            return ExecuteLockableOperationAsync(async x =>
            {
                string description = GetDescription(state.DescriptionTitle, state.DescriptionTitle);

                if (!string.IsNullOrEmpty(description))
                {
                    await WebClient.ExecuteApiRequestAsync(new ChangeStateComplaint(x.Id, state.Id, description));

                    MessageFacadeService.ShowNotificationInfo($"Решение по жалобе №{x.Id} успешно сохранено");
                }
            });
        }

        private string GetDescription(string caption, string title)
        {
            GetTextFromUserParameter fromUserParameter = new GetTextFromUserParameter(
                caption,
                title,
                isMultiline: true);

            GetTextFromUserViewModel fromUserViewModel = DialogDocumentManagerService.ShowView<GetTextFromUserViewModel>(fromUserParameter, this);

            return fromUserViewModel.IsOk ? fromUserViewModel.Content : null;
        }
    }
}