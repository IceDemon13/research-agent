using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using AutoMapper;
using DevExpress.Data.Extensions;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using DevExpress.Xpf.Core;
using Microsoft.Extensions.Logging;
using Telemart.Client.Common.Messages;
using Telemart.Client.Common.Services;
using Telemart.Client.Core.Exceptions;
using Telemart.Client.Data.Requests.Features.Bank;
using Telemart.Client.Data.Requests.Features.Employee;
using Telemart.Client.Data.Requests.Features.Organization;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.Paging;
using Telemart.Client.ViewModels.Base;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.ViewModels.Directories.Organization
{
    public sealed class OrganizationViewModel : TelemartEditorViewModelBase<OrganizationDto, OrganizationViewMessage, OrganizationViewItem>
    {
        public OrganizationViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            IMapper mapper,
            IMessenger messenger)
            : base(webClient, dictionaries, messageFacadeService, mapper, messenger)
        {
            HandleSelectionChangedCommand = new AsyncCommand<TabControlSelectionChangedEventArgs>(HandleSelectionChangedAsync);
            RefreshAccountsCommand = new AsyncCommand(RefreshAccountsAsync);
            AddAccountCommand = new DelegateCommand(AddAccount);
            EditAccountCommand = new DelegateCommand<OrganizationAccountViewItem>(EditAccount, x => x != null);
            RemoveAccountCommand = new AsyncCommand<OrganizationAccountViewItem>(RemoveAccountAsync, x => x != null);
            SetDefaultAccountCommand = new AsyncCommand<OrganizationAccountViewItem>(SetDefaultAccountAsync, x => x != null && x.Active && !x.IsDefault);
            RefreshContactsCommand = new AsyncCommand(RefreshContactsAsync);
            AddContactCommand = new DelegateCommand(AddContact);
            EditContactCommand = new DelegateCommand<OrganizationContactViewItem>(EditContact, x => x != null);
            RemoveContactCommand = new AsyncCommand<OrganizationContactViewItem>(RemoveContactAsync, x => x != null);
            SetDefaultContactCommand = new AsyncCommand<OrganizationContactViewItem>(SetDefaultContactAsync, x => x != null && !x.IsDefault);

            HandlePreviewKeyDownCommand = new DelegateCommand<KeyEventArgs>(HandlePreviewKeyDown);
        }

        public OrganizationViewModel()
        {
        }

        #region Commands

        public IAsyncCommand HandleSelectionChangedCommand { get; }

        public IAsyncCommand RefreshAccountsCommand { get; }

        public IDelegateCommand AddAccountCommand { get; }

        public IDelegateCommand EditAccountCommand { get; }

        public IAsyncCommand RemoveAccountCommand { get; }

        public IAsyncCommand SetDefaultAccountCommand { get; }

        public IAsyncCommand RefreshContactsCommand { get; }

        public IDelegateCommand AddContactCommand { get; }

        public IDelegateCommand EditContactCommand { get; }

        public IAsyncCommand RemoveContactCommand { get; }

        public IAsyncCommand SetDefaultContactCommand { get; }

        public IDelegateCommand HandlePreviewKeyDownCommand { get; }

        #endregion

        #region INPC

        public bool IsHelpVisible
        {
            get { return GetProperty(() => IsHelpVisible); }
            set { SetProperty(() => IsHelpVisible, value); }
        }

        public ReadOnlyObservableCollection<OrganizationOwnership> Ownerships
        {
            get { return GetProperty(() => Ownerships); }
            private set { SetProperty(() => Ownerships, value); }
        }

        public ObservableCollection<OrganizationAccountViewItem> Accounts
        {
            get { return GetProperty(() => Accounts); }
            set { SetProperty(() => Accounts, value); }
        }

        public OrganizationAccountViewItem CurrentAccount
        {
            get { return GetProperty(() => CurrentAccount); }
            set { SetProperty(() => CurrentAccount, value); }
        }

        public ObservableCollection<OrganizationContactViewItem> Contacts
        {
            get { return GetProperty(() => Contacts); }
            set { SetProperty(() => Contacts, value); }
        }

        public OrganizationContactViewItem CurrentContact
        {
            get { return GetProperty(() => CurrentContact); }
            set { SetProperty(() => CurrentContact, value); }
        }

        public ObservableCollection<BankDto> Banks
        {
            get { return GetProperty(() => Banks); }
            set { SetProperty(() => Banks, value); }
        }

        public ReadOnlyObservableCollection<EmployeeDto> Employees
        {
            get { return GetProperty(() => Employees); }
            private set { SetProperty(() => Employees, value); }
        }

        public bool IsCurrentUserEditable => WebClient.AuthenticatedEmployee.HasAnyRole(Role.Accountant, Role.Admin, Role.TechSupport);

        #endregion

        protected override string CreatedActionMessage { get; } = "создана";

        protected override string EntityName { get; } = "Организация";

        protected override string UpdatedActionMessage { get; } = "сохранена";

        protected override Task<Result<OrganizationDto>> CreateEntityAsync()
        {
            throw new NotSupportedException();
        }

        protected override object CreateEntityMessage(OrganizationDto dto, MessageType messageType)
        {
            return new OrganizationMessage(dto, messageType);
        }

        protected override Task<OrganizationDto> GetEntityAsync(int id)
        {
            return WebClient.ExecuteApiRequestAsync(new QueryOrganization(id));
        }

        protected override Task HandleLoadedAsync()
        {
            Ownerships = Dictionaries.GetItems<OrganizationOwnership>().ToReadOnlyObservableCollection();

            return base.HandleLoadedAsync();
        }

        protected override Task<LockResponse<OrganizationDto>> LockEntityAsync(int id)
        {
            return WebClient.ExecuteApiRequestAsync(new LockOrganization(id));
        }

        protected override Task<LockResponse<OrganizationDto>> UnlockEntityAsync(int id)
        {
            return WebClient.ExecuteApiRequestAsync(new UnlockOrganization(id));
        }

        protected override void SetCreateTitle()
        {
            throw new NotSupportedException();
        }

        protected override void SetEditTitle()
        {
            Title = $"Организация {Model.Name} ({Model.Id})";
        }

        protected override Task<Result<OrganizationDto>> UpdateEntityAsync()
        {
            OrganizationSaveDto organizationSaveDto = Mapper.Map<OrganizationSaveDto>(Model);
            return WebClient.ExecuteApiRequestAsync(new UpdateOrganization(organizationSaveDto));
        }

        private Task HandleSelectionChangedAsync(TabControlSelectionChangedEventArgs e)
        {
            switch (e.NewSelectedIndex)
            {
                case 1:
                    IsHelpVisible = true;

                    if (Accounts == null)
                    {
                        RefreshAccountsCommand.Execute(null);
                    }

                    break;
                case 2:
                    IsHelpVisible = true;

                    if (Contacts == null)
                    {
                        RefreshContactsCommand.Execute(null);
                    }

                    break;

                default:
                    IsHelpVisible = false;
                    break;
            }

            return Task.CompletedTask;
        }

        private void HandlePreviewKeyDown(KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                OkCommand.Execute(null);
            }
            else if (e.Key == Key.Escape)
            {
                CancelCommand.Execute(null);
            }
        }

        private async Task SetDefaultAccountAsync(OrganizationAccountViewItem arg)
        {
            if (Accounts.Any(x => x.CurrencyId == arg.CurrencyId && x.IsDefault))
            {
                if (!MessageFacadeService.Confirm($"Для валюты {arg.Currency.Title} уже есть счет по-умолчанию. Продолжить?"))
                {
                    return;
                }
            }

            try
            {
                Result<OrganizationAccountDto> result = await WebClient.ExecuteApiRequestAsync(new SetDefaultOrganizationAccount(arg.OrganizationId, arg.Id));
                OrganizationAccountDto accountFromServer = result.Data;

                int index = Accounts.FindIndex(x => x.Id == accountFromServer.Id);
                Accounts.RemoveAt(index);
                OrganizationAccountViewItem updatedViewItem = Mapper.Map<OrganizationAccountViewItem>(accountFromServer);
                Accounts.Insert(index, updatedViewItem);

                CurrentAccount = updatedViewItem;

                await RefreshAccountsAsync();
                MessageFacadeService.ShowNotificationInfo($"Счет '{accountFromServer.Account}' теперь счет по-умолчанию для {Model.Name}");
            }
            catch (UnexpectedSatusException exception)
            {
                Logger.LogError(exception, "Failed to set default organization account");
                ShowValidationResultView("Ошибки при обновлении счета по-умолчанию", exception.GetErrorItems());
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Failed to set account as default");
                MessageFacadeService.ShowNotificationError("Ошибка при обновлении счета по-умолчанию");
            }
        }

        private async Task RemoveAccountAsync(OrganizationAccountViewItem viewItem)
        {
            if (!MessageFacadeService.Confirm("Вы уверены?"))
            {
                return;
            }

            try
            {
                await WebClient.ExecuteApiRequestAsync(new DeleteOrganizationAccount(viewItem.OrganizationId, viewItem.Id));
                Accounts.Remove(viewItem);
                MessageFacadeService.ShowNotificationInfo($"Счет №{viewItem.Id} успешно удален");
            }
            catch (UnexpectedSatusException exception)
            {
                Logger.LogError(exception, "Failed to delete organization account");
                ShowValidationResultView("Ошибки при удалении счета", exception.GetErrorItems());
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Failed to delete organization account");
                MessageFacadeService.ShowNotificationError("Ошибка при удалении счета");
            }
        }

        private void EditAccount(OrganizationAccountViewItem viewItem)
        {
            OrganizationAccountViewModel viewModel = DialogDocumentManagerService.ShowView<OrganizationAccountViewModel>(
                new object[] { viewItem.Clone(), Model.IsVatPayer },
                this);

            if (viewModel.IsOk)
            {
                OrganizationAccountViewItem changedAccount = viewModel.ResultItem;

                viewItem.Payments = changedAccount.Payments;
                viewItem.Account = changedAccount.Account;
                viewItem.Active = changedAccount.Active;
                viewItem.BankId = changedAccount.BankId;
                viewItem.CurrencyId = changedAccount.CurrencyId;
                viewItem.IsDefault = changedAccount.IsDefault;
                viewItem.OrganizationId = changedAccount.OrganizationId;
            }
        }

        private void AddAccount()
        {
            OrganizationAccountViewItem viewItem = OrganizationAccountViewItem.Create();

            viewItem.OrganizationId = Model.Id;

            OrganizationAccountViewModel viewModel = DialogDocumentManagerService.ShowView<OrganizationAccountViewModel>(
                new object[] { viewItem, Model.IsVatPayer },
                this);

            if (viewModel.IsOk)
            {
                Accounts.Add(viewModel.ResultItem);
            }
        }

        private async Task RefreshAccountsAsync()
        {
            try
            {
                List<OrganizationAccountDto> accountsFromServer = await WebClient.ExecuteApiRequestAsync(new QueryOrganizationAccounts(Model.Id));
                IEnumerable<OrganizationAccountViewItem> accountViewItems = accountsFromServer.Select(x => Mapper.Map<OrganizationAccountViewItem>(x));
                Accounts = new ObservableCollection<OrganizationAccountViewItem>(accountViewItems);

                PagedResult<BankDto> banksFromServer = await WebClient.ExecuteApiRequestAsync(new QueryBanks(), true);
                Banks = new ObservableCollection<BankDto>(banksFromServer.Data);
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Error while loading organization accounts");
                MessageFacadeService.ShowNotificationWarning("Ошибка при загрузке счетов");
            }
        }

        private async Task SetDefaultContactAsync(OrganizationContactViewItem arg)
        {
            OrganizationContactViewItem defaultContact = Contacts.FirstOrDefault(x => x.IsDefault);
            if (defaultContact != null)
            {
                EmployeeDto defaultContactEmployee = Employees.FirstOrDefault(x => x.Id == defaultContact.EmployeeId);

                EmployeeDto contactEmployee = Employees.FirstOrDefault(x => x.Id == arg.EmployeeId);

                bool exit = defaultContactEmployee != null
                    && contactEmployee != null
                    && !MessageFacadeService.Confirm($"Контакт {defaultContactEmployee.Name} уже является основным для этой организации, вы уверены, что хотите заменить его на {contactEmployee.Name}?");

                if (exit)
                {
                    return;
                }
            }

            try
            {
                Result<OrganizationContactDto> result = await WebClient.ExecuteApiRequestAsync(new SetDefaultOrganizationContact(arg.OrganizationId, arg.Id));

                OrganizationContactDto contactFromServer = result.Data;

                int index = Contacts.FindIndex(x => x.Id == contactFromServer.Id);

                Contacts.RemoveAt(index);

                OrganizationContactViewItem updatedViewItem = Mapper.Map<OrganizationContactViewItem>(contactFromServer);

                Contacts.Insert(index, updatedViewItem);

                CurrentContact = updatedViewItem;

                EmployeeDto contactEmployee = Employees.FirstOrDefault(x => x.Id == contactFromServer.EmployeeId);

                await RefreshContactsAsync();

                MessageFacadeService.ShowNotificationInfo($"Контакт '{contactEmployee?.Name}' теперь основной для {Model.Name}");
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Failed to set contact as default");
                MessageFacadeService.ShowNotificationError("Ошибка при обновлении контакта по-умолчанию");
            }
        }

        private async Task RemoveContactAsync(OrganizationContactViewItem viewItem)
        {
            if (!MessageFacadeService.Confirm("Вы уверены?"))
            {
                return;
            }

            try
            {
                await WebClient.ExecuteApiRequestAsync(new DeleteOrganizationContact(viewItem.OrganizationId, viewItem.Id));
                Contacts.Remove(viewItem);
                MessageFacadeService.ShowNotificationInfo($"Контакт '{viewItem.Id}' успешно удален");
            }
            catch (UnexpectedSatusException exception)
            {
                Logger.LogError(exception, "Failed to delete  organization account");
                ShowValidationResultView("Ошибки при удалении контакта", exception.GetErrorItems());
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Failed to delete organization contact");
                MessageFacadeService.ShowNotificationError("Ошибка при удалении контакта");
            }
        }

        private void EditContact(OrganizationContactViewItem viewItem)
        {
            OrganizationContactViewModel viewModel = DialogDocumentManagerService.ShowView<OrganizationContactViewModel>(viewItem.Clone(), this);

            if (viewModel.IsOk)
            {
                OrganizationContactViewItem changedContact = viewModel.ResultItem;

                viewItem.PositionId = changedContact.PositionId;
                viewItem.EmployeeId = changedContact.EmployeeId;
                viewItem.IsDefault = changedContact.IsDefault;
                viewItem.OrganizationId = changedContact.OrganizationId;
            }
        }

        private void AddContact()
        {
            OrganizationContactViewItem viewItem = OrganizationContactViewItem.Create();
            viewItem.OrganizationId = Model.Id;

            OrganizationContactViewModel viewModel = DialogDocumentManagerService.ShowView<OrganizationContactViewModel>(viewItem, this);

            if (viewModel.IsOk)
            {
                Contacts.Add(viewModel.ResultItem);
            }
        }

        private async Task RefreshContactsAsync()
        {
            try
            {
                await RefreshEmployeesAsync();

                PagedResult<OrganizationContactDto> contactsFromServer = await WebClient.ExecuteApiRequestAsync(new QueryOrganizationContacts(Model.Id));
                IEnumerable<OrganizationContactViewItem> viewItems = contactsFromServer.Data.Select(x => Mapper.Map<OrganizationContactViewItem>(x));
                Contacts = new ObservableCollection<OrganizationContactViewItem>(viewItems);
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Failed to refresh employees in OrganizationViewModel");
                MessageFacadeService.ShowNotificationError("Ошибка при загрузке данных");
            }
        }

        private async Task RefreshEmployeesAsync()
        {
            List<EmployeeDto> employees = await WebClient.ExecuteApiRequestAsync(new QueryEmployees(), true).GetPagedResultDataAsync();
            Employees = employees.Where(x => x.Active).OrderBy(x => x.Name).ToReadOnlyObservableCollection();
        }
    }
}