using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using Telemart.Client.Business;
using Telemart.Client.Common;
using Telemart.Client.Common.Messages;
using Telemart.Client.Common.MvvmEnhancements;
using Telemart.Client.Common.Services;
using Telemart.Client.Core.Exceptions;
using Telemart.Client.Data.Requests.Features.City;
using Telemart.Client.Data.Requests.Features.Contractor;
using Telemart.Client.Data.Requests.Features.Customer;
using Telemart.Client.Data.Requests.Features.Customer.Actions;
using Telemart.Client.Data.Requests.Features.CustomerBonus;
using Telemart.Client.Data.Requests.Features.Employee;
using Telemart.Client.Data.Requests.Features.Hashtag;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Data.WebClient.Security;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.Properties;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.City;
using Telemart.Client.TransferObjects.Customer;
using Telemart.Client.ViewModels.Base;
using Telemart.Client.ViewModels.Validation;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.ViewModels.Customer
{
    public class CustomerViewModel : TelemartEditorViewModelBase<CustomerDto, CustomerParameter, CustomerViewItem>
    {
        private IReadOnlyDictionary<int, string> _employeeNames;

        public CustomerViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            IMapper mapper,
            IMessenger messenger,
            DocumentCommands documentCommands)
            : base(webClient, dictionaries, messageFacadeService, mapper, messenger)
        {
            DocumentCommands = documentCommands;
            AddBonusesCommand = new DelegateCommand(AddBonuses);
            DeleteBonusesCommand = new DelegateCommand(DeleteBonuses, () => Model?.Bonuses.Any() == true);
            TransferBonusesCommand = new AsyncCommand(TransferBonusesAsync, () => Model?.Bonuses.Any() == true);
            HandleRowBonusHistoryDoubleClickCommand = new DelegateCommand<CustomerBonusLogViewItem>(HandleRowBonusHistoryDoubleClick);
        }

        public CustomerViewModel()
        {
        }

        public IDelegateCommand AddBonusesCommand { get; }

        public IDelegateCommand DeleteBonusesCommand { get; }

        public IDelegateCommand HandleRowBonusHistoryDoubleClickCommand { get; }

        public IAsyncCommand TransferBonusesCommand { get; }

        public ReadOnlyObservableCollection<ComboBoxItem> Employees
        {
            get { return GetProperty(() => Employees); }
            private set { SetProperty(() => Employees, value); }
        }

        public ReadOnlyObservableCollection<ComboBoxItem> Contractors
        {
            get { return GetProperty(() => Contractors); }
            private set { SetProperty(() => Contractors, value); }
        }

        public ReadOnlyObservableCollection<ComboBoxItem> PlusHashtags
        {
            get { return GetProperty(() => PlusHashtags); }
            private set { SetProperty(() => PlusHashtags, value); }
        }

        public ReadOnlyObservableCollection<ComboBoxItem> MinusHashtags
        {
            get { return GetProperty(() => MinusHashtags); }
            private set { SetProperty(() => MinusHashtags, value); }
        }

        public ReadOnlyObservableCollection<ComboBoxItem> Cities
        {
            get { return GetProperty(() => Cities); }
            private set { SetProperty(() => Cities, value); }
        }

        public IEnumerable<SummaryViewItem> SummaryItems
        {
            get { return GetProperty(() => SummaryItems); }
            private set { SetProperty(() => SummaryItems, value); }
        }

        public ReadOnlyObservableCollection<BonusType> BonusTypes
        {
            get { return GetProperty(() => BonusTypes); }
            private set { SetProperty(() => BonusTypes, value); }
        }

        public ReadOnlyObservableCollection<CustomerBonusLogViewItem> BonusLogs
        {
            get { return GetProperty(() => BonusLogs); }
            set { SetProperty(() => BonusLogs, value); }
        }

        public CustomerBonusViewItem SelectedBonus
        {
            get { return GetProperty(() => SelectedBonus); }
            set { SetProperty(() => SelectedBonus, value); }
        }

        public ObservableCollection<CustomerBonusViewItem> AllowBonuses
        {
            get { return GetProperty(() => AllowBonuses); }
            set { SetProperty(() => AllowBonuses, value); }
        }

        public ReadOnlyObservableCollection<CustomerBonusLogTypeDto> CustomerBonusLogTypes
        {
            get { return GetProperty(() => CustomerBonusLogTypes); }
            set { SetProperty(() => CustomerBonusLogTypes, value); }
        }

        public DocumentCommands DocumentCommands { get; }

        public bool AssembliesTabVisible => Model?.Assemblies?.Any() == true;

        public bool CanEditPhone => WebClient.IsOperationAllowed(BusinessOperation.CustomerEditPhone);

        public bool CanEditEmail => WebClient.IsOperationAllowed(BusinessOperation.CustomerEditEmail);

        public bool CanEditVerificationTries => WebClient.IsOperationAllowed(BusinessOperation.CustomerEditValidationTries);

        public bool AllowEditNotCountBonuses => WebClient.IsOperationAllowed(BusinessOperation.CustomerAllowEditNotCountBonuses) && IsNewOrIsLockedByCurrentEmployee;

        protected override string CreatedActionMessage { get; } = "создан";

        protected override string EntityName { get; } = "Клиент";

        protected override string UpdatedActionMessage { get; } = "сохранен";

        protected override Task<Result<CustomerDto>> CreateEntityAsync()
        {
            throw new NotSupportedException();
        }

        protected override void SetCreateTitle()
        {
            throw new NotSupportedException();
        }

        protected override async Task HandleLoadedAsync()
        {
            await base.HandleLoadedAsync();

            await Task.WhenAll(RefreshContractors(), RefreshEmployees(), RefreshCities(), RefreshHashtags(), GetCustomerBonusLogTypesAsync(), RefreshBonusLogsAsync());

            SummaryItems = GetSummaryItems();

            BonusTypes = Dictionaries.GetItems<BonusType>().ToReadOnlyObservableCollection();

            RaisePropertiesChanged(
                nameof(AssembliesTabVisible),
                nameof(CanEditEmail),
                nameof(CanEditPhone),
                nameof(CanEditVerificationTries),
                nameof(AllowEditNotCountBonuses));

            async Task RefreshEmployees()
            {
                List<EmployeeDto> employees = await WebClient.ExecuteApiRequestAsync(new QueryEmployees(), true).GetPagedResultDataAsync();

                _employeeNames = employees.ToDictionary(x => x.Id, y => y.Name);

                Employees = employees
                    .Where(x => x.Active || x.Id == Model.EmployeeId)
                    .OrderBy(x => x.Name)
                    .Select(x => new ComboBoxItem(x.Id, x.Name))
                    .ToReadOnlyObservableCollection();
            }

            async Task RefreshContractors()
            {
                List<ContractorDto> contractors = await WebClient.ExecuteApiRequestAsync(new QueryContractors(), true).GetPagedResultDataAsync();

                Contractors = contractors
                    .Where(x => (x.Active && x.IsClient && !x.IsFolder) || x.Id == Model.ContractorId)
                    .OrderBy(x => x.Name)
                    .Select(x => new ComboBoxItem(x.Id, x.Name))
                    .ToReadOnlyObservableCollection();
            }

            async Task RefreshCities()
            {
                List<CityDto> cities = await WebClient.ExecuteApiRequestAsync(new QueryCities(), true).GetPagedResultDataAsync();

                Cities = cities
                    .Select(x => new ComboBoxItem(x.Id, x.Name))
                    .ToReadOnlyObservableCollection();
            }

            async Task RefreshHashtags()
            {
                List<HashtagDto> hashtags = await WebClient.ExecuteApiRequestAsync(new QueryHashtags());

                PlusHashtags = hashtags
                    .Where(x => x.TypeId == HashtagType.Plus.Id && (x.Active || Model.PlusHashtagIds?.Contains(x.Id) == true))
                    .Select(x => new ComboBoxItem(x.Id, x.Name))
                    .ToReadOnlyObservableCollection();

                MinusHashtags = hashtags
                    .Where(x => x.TypeId == HashtagType.Minus.Id && (x.Active || Model.PlusHashtagIds?.Contains(x.Id) == true))
                    .Select(x => new ComboBoxItem(x.Id, x.Name))
                   .ToReadOnlyObservableCollection();
            }

            async Task GetCustomerBonusLogTypesAsync()
            {
                List<CustomerBonusLogTypeDto> bonusLogTypes = await WebClient.ExecuteApiRequestAsync(new QueryBonusLogTypes());

                CustomerBonusLogTypes = bonusLogTypes.ToReadOnlyObservableCollection();
            }
        }

        private void AddAllowTypeBonuses()
        {
            AllowBonuses = Model.Bonuses.ToObservableRangeCollection();
        }

        private async Task RefreshBonusLogsAsync()
        {
            List<CustomerBonusLogDto> bonusLogs = await WebClient.ExecuteApiRequestAsync(new QueryCustomerBonusLogs(Model.Id));

            List<CustomerBonusLogViewItem> bonusLogsItems = bonusLogs
                .Select(x => Mapper.Map<CustomerBonusLogViewItem>(x)).OrderByDescending(x => x.CreatedOn).ToList();

            bonusLogsItems.ForEach(x =>
            {
                if (x.Data is not null)
                {
                    if (x.Data.TryGetValue("id_order", out JToken document) && int.TryParse(document.ToString(), out int documentId))
                    {
                        x.DocumentId = documentId;
                    }

                    if (x.Data.TryGetValue("id_service_request", out document) && int.TryParse(document.ToString(), out documentId))
                    {
                        x.DocumentId = documentId;
                    }
                }
            });

            BonusLogs = bonusLogsItems.ToReadOnlyObservableCollection();
        }

        protected override object CreateEntityMessage(CustomerDto dto, MessageType messageType)
        {
            return new CustomerMessage(dto, messageType);
        }

        protected override Task<CustomerDto> GetEntityAsync(int id)
        {
            return WebClient.ExecuteApiRequestAsync(new QueryCustomer(id));
        }

        protected override Task<LockResponse<CustomerDto>> LockEntityAsync(int id)
        {
            return WebClient.ExecuteApiRequestAsync(new LockCustomer(id));
        }

        protected override Task<LockResponse<CustomerDto>> UnlockEntityAsync(int id)
        {
            return WebClient.ExecuteApiRequestAsync(new UnlockCustomer(id));
        }

        protected override void SetEditTitle()
        {
            Title = $"{Model.Fio} ({Model.Id})";
        }

        protected override Task<Result<CustomerDto>> UpdateEntityAsync()
        {
            List<int> hashtagIds = Model.PlusHashtagIds
                .Concat(Model.MinusHashtagIds)
                .ToList();

            CustomerSaveDto dto = new CustomerSaveDto(Model.Id, Model.EmployeeId, Model.ContractorId!.Value, Model.Phone1, Model.Phone2, Model.Email, hashtagIds, Model.ValidationTries, Model.NotCountBonuses);

            return WebClient.ExecuteApiRequestAsync(new UpdateCustomer(dto));
        }

        protected override void AfterSetData()
        {
            if (_employeeNames != null)
            {
                SummaryItems = GetSummaryItems();
            }

            AddAllowTypeBonuses();

            base.AfterSetData();

            Model.Assemblies = Model.Assemblies.OrderBy(x => x.CustomerDeleted).ThenByDescending(x => x.CreatedOn).ToObservableCollection();
            ModelOriginal.Assemblies = ModelOriginal.Assemblies.OrderBy(x => x.CustomerDeleted).ThenByDescending(x => x.CreatedOn).ToObservableCollection();

            RaisePropertiesChanged(nameof(AllowEditNotCountBonuses));
        }

        private void AddBonuses()
        {
            CustomerEditBonusesParameter parameter = new CustomerEditBonusesParameter(
                GetAllowedBonusTypes(),
                BonusTypes.FirstOrDefault(x => x.Id == SelectedBonus?.BonusTypeId),
                "Начисление бонусов",
                true,
                CustomerAddBonusesAsync);

            DialogDocumentManagerService.ShowView<CustomerEditBonusesViewModel>(parameter, this);
        }

        private void HandleRowBonusHistoryDoubleClick(CustomerBonusLogViewItem item)
        {
            if (item is null || item.DocumentId is null)
            {
                return;
            }

            try
            {
                switch (item.TypeId)
                {
                    case CustomerBonusLogTypeIds.OrderId:
                        Messenger.Send(new OrderEditViewMessage(item.DocumentId.Value));
                        break;
                    case CustomerBonusLogTypeIds.ServiceRequestId:
                        Messenger.Send(new ServiceRequestViewMessage(item.DocumentId.Value));
                        break;
                }
            }
            catch (Exception ex)
            {
               Logger.LogError(ex, "Failed to open document.");
               MessageFacadeService.ShowNotificationError("Непредвиденная ошибка");
            }
        }

        private async Task<bool> CustomerAddBonusesAsync((int bonusTypeId, int quantity, DateTime? expireDate) parameter)
        {
            bool success = false;

            const string errorRu = "Ошибка при начислении бонусов";
            const string errorEn = "Failed to add bonuses";
            try
            {
                Result<CustomerDto> result = await WebClient.ExecuteApiRequestAsync(new CustomerAddBonuses(Model.Id, parameter.quantity, parameter.bonusTypeId, parameter.expireDate));

                CustomerBonusViewItem customerBonus = Model.Bonuses.FirstOrDefault(x => x.BonusTypeId == parameter.bonusTypeId);

                if (customerBonus is null)
                {
                    Model.Bonuses.Add(new CustomerBonusViewItem()
                    {
                        BonusTypeId = parameter.bonusTypeId,
                        CustomerId = Model.Id,
                        Quantity = parameter.quantity,
                        BonusTypeName = BonusTypes.First(x => x.Id == parameter.bonusTypeId).Name
                    });
                }
                else
                {
                    customerBonus.Quantity += parameter.quantity;
                }

                await RefreshBonusLogsAsync();

                AddAllowTypeBonuses();

                MessageFacadeService.ShowNotificationInfo("Бонусы успешно начислены");
                success = true;
            }
            catch (UnexpectedSatusException exception)
            {
                MessageFacadeService.ShowNotificationError(errorRu);
                ShowValidationResultView(errorRu, exception.GetErrorItems());
            }
            catch (UnexpectedErrorException exception)
            {
                Logger.LogError(exception, errorEn);
                ShowValidationResultView(Resources.ServerConnectError, new[] { new ValidationResultItem(Resources.ServerUnavailable, true) });
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, errorEn);
                MessageFacadeService.ShowNotificationError(errorRu);
            }

            return success;
        }

        private void DeleteBonuses()
        {
            if (!IsAllowSelectedBonus("списание"))
            {
                return;
            }

            CustomerEditBonusesParameter parameter = new CustomerEditBonusesParameter(
                GetAllowedBonusTypes(),
                BonusTypes.FirstOrDefault(x => x.Id == SelectedBonus?.BonusTypeId),
                "Списание бонусов",
                false,
                CustomerDeleteBonusesAsync);

            DialogDocumentManagerService.ShowView<CustomerEditBonusesViewModel>(parameter, this);
        }

        private async Task<bool> CustomerDeleteBonusesAsync((int bonusTypeId, int quantity, DateTime?_) parameter)
        {
            bool success = false;

            const string errorRu = "Ошибка при списании бонусов";
            const string errorEn = "Failed to delete bonuses";

            try
            {
                Result<CustomerDto> result = await WebClient.ExecuteApiRequestAsync(new CustomerDeleteBonuses(Model.Id, parameter.quantity, parameter.bonusTypeId));

                CustomerBonusViewItem customerBonus = Model.Bonuses.FirstOrDefault(x => x.BonusTypeId == parameter.bonusTypeId);
                customerBonus.Quantity -= parameter.quantity;

                MessageFacadeService.ShowNotificationInfo("Бонусы успешно списаны");
                success = true;

                await RefreshBonusLogsAsync();

                AddAllowTypeBonuses();
            }
            catch (UnexpectedSatusException exception)
            {
                MessageFacadeService.ShowNotificationError(errorRu);
                ShowValidationResultView(errorRu, exception.GetErrorItems());
            }
            catch (UnexpectedErrorException exception)
            {
                Logger.LogError(exception, errorEn);
                ShowValidationResultView(Resources.ServerConnectError, new[] { new ValidationResultItem(Resources.ServerUnavailable, true) });
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, errorEn);
                MessageFacadeService.ShowNotificationError(errorRu);
            }

            return success;
        }

        private async Task TransferBonusesAsync()
        {
            if (!IsAllowSelectedBonus("перевод"))
            {
                return;
            }

            CustomerTransferBonusesViewModel viewModel = DialogDocumentManagerService
                .ShowView<CustomerTransferBonusesViewModel>(
                    new CustomerTransferBonusesParameter(
                        Model.Id,
                        BonusTypes.FirstOrDefault(x => x.Id == SelectedBonus?.BonusTypeId),
                        GetAllowedBonusTypes()),
                    this);

            if (!viewModel.IsOk)
            {
                return;
            }

            SelectedBonus.Quantity -= viewModel.Quantity;

            await RefreshBonusLogsAsync();
        }

        private IEnumerable<SummaryViewItem> GetSummaryItems()
        {
            yield return new SummaryViewItem("Создал", $"{_employeeNames.GetValueOrDefault(Model.CreatedBy)} ({Model.CreatedOn.ToString(DateFormattingRules.FullDateTimeFormat)})");
            yield return new SummaryViewItem("Изменил", $"{_employeeNames.GetValueOrDefault(Model.ModifiedBy)} ({Model.ModifiedOn.ToString(DateFormattingRules.FullDateTimeFormat)})");

            if (Model.Birthday.HasValue)
            {
                yield return new SummaryViewItem("ДР", Model.Birthday.Value.ToString("dd.MM.yyyy"));
            }

            yield return new SummaryViewItem("Активирован", Model.Validated ? "Да" : "Нет");
        }

        private ReadOnlyObservableCollection<BonusType> GetAllowedBonusTypes()
        {
            return BonusTypes.Where(x => (x.Id == BonusType.DragonPointsId && WebClient.IsOperationAllowed(BusinessOperation.AllowChangeDragonPoints))
                                  || (x.Id == BonusType.TelemartPointsId && WebClient.IsOperationAllowed(BusinessOperation.AllowChangeTelemartPoints))
                                  || (x.Id == BonusType.TradeInPointsId && WebClient.IsOperationAllowed(BusinessOperation.AllowChangeTradeInPoints)))
                .ToReadOnlyObservableCollection();
        }

        private bool IsAllowSelectedBonus(string actionType)
        {
            if (SelectedBonus?.BonusTypeId == BonusType.DragonPointsId && !WebClient.IsOperationAllowed(BusinessOperation.AllowChangeDragonPoints))
            {
                MessageFacadeService.ShowNotificationWarning($"У вас нет прав на {actionType} бонусов типа DragonPoints");
                return false;
            }

            if (SelectedBonus?.BonusTypeId == BonusType.TelemartPointsId && !WebClient.IsOperationAllowed(BusinessOperation.AllowChangeTelemartPoints))
            {
                MessageFacadeService.ShowNotificationWarning($"У вас нет прав на {actionType} бонусов типа TelemartPoints");
                return false;
            }

            if (SelectedBonus?.BonusTypeId == BonusType.TradeInPointsId && !WebClient.IsOperationAllowed(BusinessOperation.AllowChangeTradeInPoints))
            {
                MessageFacadeService.ShowNotificationWarning($"У вас нет прав на {actionType} бонусов типа{Environment.NewLine}Trade-In Points");
                return false;
            }

            return true;
        }
    }
}