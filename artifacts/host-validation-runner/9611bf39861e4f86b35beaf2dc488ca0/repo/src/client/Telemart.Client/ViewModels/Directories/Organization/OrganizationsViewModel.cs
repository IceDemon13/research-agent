using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using AutoMapper;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using Microsoft.Extensions.Logging;
using Telemart.Client.Common.Messages;
using Telemart.Client.Common.Services;
using Telemart.Client.Core.Exceptions;
using Telemart.Client.Data.Requests.Features.Organization;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.Properties;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.Paging;
using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.Directories.Organization
{
    public class OrganizationsViewModel : TelemartViewModelBase, ISupportHotkeys
    {
        public OrganizationsViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            IMapper mapper,
            IMessenger messenger)
            : base(webClient, dictionaries, messageFacadeService)
        {
            Mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            Messenger = messenger ?? throw new ArgumentNullException(nameof(messenger));

            RefreshCommand = new AsyncCommand(RefreshAsync);
            AddCommand = new DelegateCommand(AddOrganization);
            EditCommand = new DelegateCommand<OrganizationViewItem>(EditOrganization, x => x != null);

            OrganizationPositions = Dictionaries.GetItems<OrganizationPosition>().ToReadOnlyObservableCollection();
            OrganizationOwnerships = Dictionaries.GetItems<OrganizationOwnership>().ToReadOnlyObservableCollection();

            Messenger.Register<OrganizationMessage>(this, OnOrganizationMessage);
        }

        #region INPC

        public ObservableCollection<OrganizationViewItem> Organizations
        {
            get { return GetProperty(() => Organizations); }
            set { SetProperty(() => Organizations, value); }
        }

        public OrganizationViewItem CurrentOrganization
        {
            get { return GetProperty(() => CurrentOrganization); }
            set { SetProperty(() => CurrentOrganization, value); }
        }

        public ReadOnlyObservableCollection<OrganizationPosition> OrganizationPositions
        {
            get { return GetProperty(() => OrganizationPositions); }
            private set { SetProperty(() => OrganizationPositions, value); }
        }

        public ReadOnlyObservableCollection<OrganizationOwnership> OrganizationOwnerships
        {
            get { return GetProperty(() => OrganizationOwnerships); }
            private set { SetProperty(() => OrganizationOwnerships, value); }
        }

        public bool IsColumnChooserVisible
        {
            get { return GetProperty(() => IsColumnChooserVisible); }
            set { SetProperty(() => IsColumnChooserVisible, value); }
        }

        public bool IsCurrentUserEditable => WebClient.AuthenticatedEmployee.HasAnyRole(Role.Accountant, Role.Admin, Role.TechSupport);

        #endregion

        #region Commands

        public IDelegateCommand AddCommand { get; }

        public IDelegateCommand EditCommand { get; }

        public IAsyncCommand RefreshCommand { get; set; }

        #endregion

        private IMapper Mapper { get; }

        private IMessenger Messenger { get; }

        private IDocumentManagerService DialogDocumentManagerService => GetService<IDocumentManagerService>("DialogDocumentManagerService", ServiceSearchMode.PreferParents);

        public bool HandleHotkey(HotkeyMessage hotkeyMessage)
        {
            bool handled = false;

            switch (hotkeyMessage.Key)
            {
                case Key.Insert:
                    {
                        AddCommand.Execute(null);
                        handled = true;
                        break;
                    }

                case Key.F2:
                    {
                        EditCommand.Execute(CurrentOrganization);
                        handled = true;
                        break;
                    }

                case Key.F5:
                    {
                        RefreshCommand.Execute(null);
                        handled = true;
                        break;
                    }

                case Key.F6:
                    {
                        IsColumnChooserVisible = !IsColumnChooserVisible;
                        handled = true;
                        break;
                    }
            }

            return handled;
        }

        protected override Task HandleLoadedAsync()
        {
            RefreshCommand.Execute(null);
            return Task.CompletedTask;
        }

        private void EditOrganization(OrganizationViewItem item)
        {
            Messenger.Send(new OrganizationViewMessage(item.Id));
        }

        private void AddOrganization()
        {
            CreateOrganizationViewModel viewModel = DialogDocumentManagerService.ShowView<CreateOrganizationViewModel>(null, this);

            if (viewModel.IsOk)
            {
                OrganizationViewItem viewItem = Mapper.Map<OrganizationViewItem>(viewModel.ResultOrganization);
                Organizations.Insert(0, viewItem);
                MessageFacadeService.ShowNotificationInfo($"Организация №{viewItem.Id} успешно создана");
                CurrentOrganization = viewItem;
            }
        }

        private async Task RefreshAsync()
        {
            try
            {
                Organizations?.Clear();

                PagedResult<OrganizationDto> pagedResult = await WebClient.ExecuteApiRequestAsync(new QueryOrganizations());

                Organizations = pagedResult.Data.Select(x => Mapper.Map<OrganizationViewItem>(x)).ToObservableCollection();
            }
            catch (UnexpectedSatusException exception)
            {
                Logger.LogError(exception, "Failed to refresh OrganizationsViewModel");
                MessageFacadeService.ShowNotificationError(Resources.ErrorDuringDataLoading);
            }
            catch (UnexpectedErrorException exception)
            {
                Logger.LogError(exception, "Failed to refresh OrganizationsViewModel");
                MessageFacadeService.ShowNotificationError(Resources.ServerConnectError);
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Failed to refresh OrganizationsViewModel");
                MessageFacadeService.ShowNotificationError(Resources.ErrorDuringDataLoading);
            }
        }

        private void OnOrganizationMessage(OrganizationMessage message)
        {
            switch (message.MessageType)
            {
                case MessageType.Changed:
                    {
                        Organizations.DoActionWithItem(x => x.Id == message.Entity.Id, viewItem => Mapper.Map(message.Entity, viewItem));
                        break;
                    }
            }
        }
    }
}