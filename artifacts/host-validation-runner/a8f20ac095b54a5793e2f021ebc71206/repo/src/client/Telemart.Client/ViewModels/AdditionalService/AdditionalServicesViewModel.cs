using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using Microsoft.Extensions.Logging;
using Telemart.Client.Common.Messages;
using Telemart.Client.Common.MvvmEnhancements;
using Telemart.Client.Common.Services;
using Telemart.Client.Core.Exceptions;
using Telemart.Client.Data.Requests.Features.AdditionalService;
using Telemart.Client.Data.Requests.Features.AdditionalService.Actions;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.Properties;
using Telemart.Client.TransferObjects;
using Telemart.Client.ViewModels.Base;
using Telemart.Client.ViewModels.Common;
using Telemart.Client.ViewModels.Dialogs;
using Telemart.Client.ViewModels.Validation;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.ViewModels.AdditionalService
{
    public sealed class AdditionalServicesViewModel : TelemartViewModelBase, ISupportHotkeys
    {
        private List<AdditionalServicesViewItem> allServices;
        private List<AdditionalServiceGroupViewItem> allGroups;

        public AdditionalServicesViewModel(
          IWebClient webClient,
          IDictionaries dictionaries,
          IMessageFacadeService messageFacadeService,
          IMapper mapper,
          IMessenger messenger)
          : base(webClient, dictionaries, messageFacadeService)
        {
            Messenger = messenger;
            Mapper = mapper;

            RefreshCommand = new AsyncCommand(RefreshAsync);
            EditCommand = new DelegateCommand(Edit, () => SelectedElement != null);
            AddCommand = new DelegateCommand(Add, () => SelectedElement is AdditionalServiceGroupViewItem item && item.Groups?.Any() != true);
            AddGroupCommand = new DelegateCommand(AddGroup, () => (SelectedElement is AdditionalServiceGroupViewItem item && item.AdditionalServices?.Any() != true) || SelectedElement == null);
            ReorderCommand = new DelegateCommand(Reorder, () => SelectedGroup != null || SelectedService != null);
            ReorderPriorityCommand = new DelegateCommand(ReorderPriority, () => AdditionalServiceGroups != null && AdditionalServiceGroups.Any());
            MoveCommand = new AsyncCommand(MoveAsync, () => SelectedElement != null);

            Messenger.Register<EntityMessage<AdditionalServiceGroupDto>>(this, OnAdditionalServiceGroupMessage);
            Messenger.Register<EntityMessage<AdditionalServiceDto>>(this, OnAdditionalServiceMessage);

            allServices = new List<AdditionalServicesViewItem>();
            allGroups = new List<AdditionalServiceGroupViewItem>();

            ShowInactiveServices = false;
        }

        public AdditionalServicesViewModel()
        {
        }

        #region Commands

        public IDelegateCommand ReorderCommand { get; }

        public IDelegateCommand ReorderPriorityCommand { get; }

        public IAsyncCommand RefreshCommand { get; }

        public IAsyncCommand MoveCommand { get; }

        public IDelegateCommand EditCommand { get; }

        public IDelegateCommand AddCommand { get; }

        public IDelegateCommand AddGroupCommand { get; }

        #endregion

        public ObservableCollection<AdditionalServiceGroupViewItem> AdditionalServiceGroups
        {
            get { return GetProperty(() => AdditionalServiceGroups); }
            set { SetProperty(() => AdditionalServiceGroups, value); }
        }

        public ObservableCollection<AdditionalServicePriorityType> PriorityTypes
        {
            get { return GetProperty(() => PriorityTypes); }
            set { SetProperty(() => PriorityTypes, value); }
        }

        public BindableBase SelectedElement
        {
            get { return GetProperty(() => SelectedElement); }
            set { SetProperty(() => SelectedElement, value, () => RaisePropertiesChanged(nameof(SelectedGroup), nameof(SelectedService))); }
        }

        public bool IsColumnChooserVisible
        {
            get { return GetProperty(() => IsColumnChooserVisible); }
            set { SetProperty(() => IsColumnChooserVisible, value); }
        }

        public bool ShowInactiveServices
        {
            get { return GetProperty(() => ShowInactiveServices); }
            set => SetProperty(() => ShowInactiveServices, value, () => RefreshCommand.Execute(null));
        }

        public AdditionalServiceGroupViewItem SelectedGroup => SelectedElement as AdditionalServiceGroupViewItem;

        public AdditionalServicesViewItem SelectedService => SelectedElement as AdditionalServicesViewItem;

        private IMapper Mapper { get; }

        private IMessenger Messenger { get; }

        private IDocumentManagerService SizeableDialogDocumentManagerService => GetService<IDocumentManagerService>("SizeableDialogDocumentManagerService", ServiceSearchMode.PreferParents);

        private IDocumentManagerService DialogDocumentManagerService => GetService<IDocumentManagerService>("DialogDocumentManagerService", ServiceSearchMode.PreferParents);

        public bool HandleHotkey(HotkeyMessage msg)
        {
            bool handled = false;

            switch (msg.HotkeyMessageType)
            {
                case HotkeyMessageType.Refresh:
                    RefreshCommand.Execute(null);
                    handled = true;
                    break;

                case HotkeyMessageType.Edit:
                    EditCommand.Execute(null);
                    handled = true;
                    break;

                case HotkeyMessageType.ShowColumnChooser:
                    IsColumnChooserVisible = !IsColumnChooserVisible;
                    handled = true;
                    break;
            }

            return handled;
        }

        protected override Task HandleLoadedAsync()
        {
            PriorityTypes = Dictionaries.GetItems<AdditionalServicePriorityType>().ToObservableCollection();

            RefreshCommand.Execute(null);
            return base.HandleLoadedAsync();
        }

        private static IEnumerable<AdditionalServiceGroupViewItem> GetParentGroups(IEnumerable<AdditionalServiceGroupViewItem> groups, int? parentId)
        {
            foreach (AdditionalServiceGroupViewItem group in groups.Where(x => x.ParentId == parentId))
            {
                group.Groups = GetParentGroups(groups, group.Id).OrderBy(x => x.Position).ToObservableCollection();
                group.AdditionalServices = group.AdditionalServices.OrderBy(x => x.Position).ToObservableCollection();

                yield return group;
            }
        }

        private static AdditionalServiceGroupViewItem RecursiveFindGroup(IEnumerable<AdditionalServiceGroupViewItem> groups, int id)
        {
            foreach (AdditionalServiceGroupViewItem group in groups)
            {
                if (group.Id == id)
                {
                    return group;
                }

                AdditionalServiceGroupViewItem childGroup = RecursiveFindGroup(group.Groups, id);

                if (childGroup != null)
                {
                    return childGroup;
                }
            }

            return null;
        }

        private static AdditionalServicesViewItem RecursiveFindService(IEnumerable<AdditionalServiceGroupViewItem> groups, int id)
        {
            foreach (AdditionalServiceGroupViewItem group in groups)
            {
                AdditionalServicesViewItem service = group.AdditionalServices.FirstOrDefault(x => x.Id == id);

                if (service != null)
                {
                    return service;
                }

                AdditionalServicesViewItem childService = RecursiveFindService(group.Groups, id);

                if (childService != null)
                {
                    return childService;
                }
            }

            return null;
        }

        private async Task RefreshAsync()
        {
            try
            {
                List<AdditionalServiceGroupDto> additionalServiceGroups = await WebClient.ExecuteApiRequestAsync(new QueryAdditionalServiceGroups(!ShowInactiveServices));

                allGroups = additionalServiceGroups
                     .Select(x => Mapper.Map<AdditionalServiceGroupViewItem>(x))
                     .ToList();

                allServices = allGroups.SelectMany(x => x.AdditionalServices).ToList();

                SelectedElement = null;

                AdditionalServiceGroups = GetParentGroups(allGroups, null).ToObservableCollection();
                AdditionalServiceGroups = AdditionalServiceGroups.OrderBy(x => x.Position).ToObservableCollection();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, Resources.ErrorDuringDataLoading);

                MessageFacadeService.ShowNotificationError(Resources.ErrorDuringDataLoading);
            }
        }

        private void Add()
        {
            if (SelectedGroup is null)
            {
                MessageFacadeService.ShowNotificationWarning("Выберите группу");
                return;
            }

            SizeableDialogDocumentManagerService.ShowView<AdditionalServiceViewModel>(new AdditionalServiceParameter(0, SelectedGroup.Id, SelectedGroup.Name), this);
        }

        private void AddGroup()
        {
            DialogDocumentManagerService.ShowView<AdditionalServiceGroupViewModel>(new AdditionalServiceGroupParameter(0, SelectedGroup?.Id, SelectedGroup?.Name), this);
        }

        private void Edit()
        {
            if (SelectedService != null)
            {
                SizeableDialogDocumentManagerService.ShowView<AdditionalServiceViewModel>(new AdditionalServiceParameter(SelectedService.Id, SelectedService.GroupId, SelectedService.GroupName), this);
                return;
            }

            if (SelectedGroup != null)
            {
                DialogDocumentManagerService.ShowView<AdditionalServiceGroupViewModel>(new AdditionalServiceGroupParameter(SelectedGroup.Id, SelectedGroup.ParentId, SelectedGroup.Name), this);
            }
        }

        private async Task MoveAsync()
        {
            switch (SelectedElement)
            {
                case AdditionalServiceGroupViewItem: await MoveGroupAsync(); break;
                case AdditionalServicesViewItem: await MoveServiceAsync(); break;
            }
        }

        private async Task MoveGroupAsync()
        {
            ReadOnlyObservableCollection<HierarchicalItem> groups = allGroups
                .Select(x => new HierarchicalItem(x.Id, x.Name, x.ParentId ?? 0))
                .Union(new[] { new HierarchicalItem(0, "[Корень]") })
                .ToReadOnlyObservableCollection();

            GroupMoveViewModel viewModel = DialogDocumentManagerService.ShowView<GroupMoveViewModel>(
                new GroupMoveParameter(groups, "Перенос группы услуг"),
                this);

            if (!viewModel.IsOk)
            {
                return;
            }

            try
            {
                Result<AdditionalServiceGroupDto> result = await WebClient.ExecuteApiRequestAsync(new MoveAdditionalServiceGroup(SelectedGroup.Id, viewModel.ToGroupId));

                if (result.Warnings.Any())
                {
                    IReadOnlyCollection<ValidationResultItem> validationResultItems = result
                        .Warnings
                        .Select(x => new ValidationResultItem(x, false))
                        .ToList();

                    const string message = "Группа перемещена с предупреждениями";

                    MessageFacadeService.ShowValidationResultView(message, validationResultItems, this);

                    MessageFacadeService.ShowNotificationWarning(message);
                }
                else
                {
                    RefreshCommand.Execute(null);
                    MessageFacadeService.ShowNotificationInfo("Группа успешно перемещена");
                }

                Messenger.Send(new EntityMessage<AdditionalServiceGroupDto>(result.Data, MessageType.Changed));
            }
            catch (UnexpectedSatusException exception)
            {
                MessageFacadeService.ShowNotificationError("Ошибка при перемещении группы");
                MessageFacadeService.ShowValidationResultView("Ошибки при перемещении группы", exception.GetErrorItems(), this);
            }
            catch (UnexpectedErrorException exception)
            {
                Logger.LogError(exception, "Failed to move additional service group");
                MessageFacadeService.ShowValidationResultView(Resources.ServerConnectError, new[] { new ValidationResultItem(Resources.ServerUnavailable, true) }, this);
            }
            catch (Exception exception)
            {
                MessageFacadeService.ShowNotificationError("Ошибка при перемещении группы");
                Logger.LogError(exception, "Error while moving additional service group");
            }
        }

        private async Task MoveServiceAsync()
        {
            ReadOnlyObservableCollection<HierarchicalItem> groups = allGroups
                .Select(x => new HierarchicalItem(x.Id, x.Name, x.ParentId ?? 0))
                .Union(new[] { new HierarchicalItem(0, "[Корень]") })
                .ToReadOnlyObservableCollection();

            GroupMoveViewModel viewModel = DialogDocumentManagerService.ShowView<GroupMoveViewModel>(
                new GroupMoveParameter(groups, $"Перенос услуги №{SelectedService.Id}"),
                this);

            if (!viewModel.IsOk)
            {
                return;
            }

            try
            {
                Result<AdditionalServiceDto> result = await WebClient.ExecuteApiRequestAsync(new MoveAdditionalService(SelectedService.Id, viewModel.ToGroupId));

                if (result.Warnings.Any())
                {
                    IReadOnlyCollection<ValidationResultItem> validationResultItems = result
                        .Warnings
                        .Select(x => new ValidationResultItem(x, false))
                        .ToList();

                    const string message = "Услуга перемещена с предупреждениями";

                    MessageFacadeService.ShowValidationResultView(message, validationResultItems, this);

                    MessageFacadeService.ShowNotificationWarning(message);
                }
                else
                {
                    RefreshCommand.Execute(null);
                    MessageFacadeService.ShowNotificationInfo("Услуга успешно перемещена");
                }

                Messenger.Send(new EntityMessage<AdditionalServiceDto>(result.Data, MessageType.Changed));
            }
            catch (UnexpectedSatusException exception)
            {
                MessageFacadeService.ShowNotificationError("Ошибка при перемещении услуги");
                MessageFacadeService.ShowValidationResultView("Ошибки при перемещении услуги", exception.GetErrorItems(), this);
            }
            catch (UnexpectedErrorException exception)
            {
                Logger.LogError(exception, "Failed to move additional service");
                MessageFacadeService.ShowValidationResultView(Resources.ServerConnectError, new[] { new ValidationResultItem(Resources.ServerUnavailable, true) }, this);
            }
            catch (Exception exception)
            {
                MessageFacadeService.ShowNotificationError("Ошибка при перемещении услуги");
                Logger.LogError(exception, "Error while moving additional service");
            }
        }

        private void Reorder()
        {
            if (SelectedService != null)
            {
                 ReorderServices();
            }

            if (SelectedGroup != null)
            {
                ReorderGroups();
            }
        }

        private void ReorderServices()
        {
            List<AdditionalServicesViewItem> services = allServices
                .Where(x => x.GroupId == SelectedService.GroupId)
                .OrderBy(x => x.Position)
                .ToList();

            ReorderItemsViewModel viewModel = DialogDocumentManagerService.ShowView<ReorderItemsViewModel>(
              new ReorderItemsParameter("Порядок услуг", services.Select(x => new ComboBoxItem(x.Id, x.Name)).ToList(), true, x => ReorderServicesHandleOkAsync(x, services)),
              this);
        }

        private async Task<bool> ReorderServicesHandleOkAsync(IReadOnlyCollection<ComboBoxItem> changedServices, List<AdditionalServicesViewItem> services)
        {
            bool success = false;

            Dictionary<int, int> reorderedItems = changedServices
                .Select((x, i) => new { x.Id, Position = i })
                .ToDictionary(x => x.Id, x => x.Position);

            try
            {
                IReadOnlyCollection<AdditionalServicePositionDto> positions = reorderedItems.Select(x => new AdditionalServicePositionDto(x.Key, x.Value)).ToList();

                await WebClient.ExecuteApiRequestAsync(new ReorderAdditionalServices(SelectedService.GroupId, positions.ToArray()));

                bool isChanged = false;

                foreach (AdditionalServicesViewItem service in services)
                {
                    if (reorderedItems.TryGetValue(service.Id, out int position) && service.Position != position)
                    {
                        service.Position = position;

                        isChanged = true;
                    }
                }

                if (!isChanged)
                {
                    return true;
                }

                AdditionalServiceGroupViewItem groupToReorder = RecursiveFindGroup(AdditionalServiceGroups, SelectedService.GroupId);
                groupToReorder.AdditionalServices = groupToReorder.AdditionalServices.OrderBy(x => x.Position).ToObservableCollection();

                RaisePropertyChanged(nameof(AdditionalServiceGroups));

                MessageFacadeService.ShowNotificationInfo("Позиции услуг успешно изменены");

                success = true;
            }
            catch (UnexpectedSatusException exception)
            {
                MessageFacadeService.ShowNotificationError("Ошибка при изменении позиций услуг");
                MessageFacadeService.ShowValidationResultView("Ошибки при изменении позиций услуг", exception.GetErrorItems(), this);
            }
            catch (UnexpectedErrorException exception)
            {
                Logger.LogError(exception, "Failed to reorder services");
                MessageFacadeService.ShowValidationResultView(Resources.ServerConnectError, new[] { new ValidationResultItem(Resources.ServerConnectError, true) }, this);
            }
            catch (Exception exception)
            {
                MessageFacadeService.ShowNotificationError("Ошибка при изменении позиций услуг");
                Logger.LogError(exception, "Error while reordering services");
            }

            return success;
        }

        private void ReorderGroups()
        {
            List<AdditionalServiceGroupViewItem> groups = allGroups
                .Where(x => x.ParentId == SelectedGroup.ParentId)
                .OrderBy(x => x.Position)
                .ToList();

            ReorderItemsViewModel viewModel = DialogDocumentManagerService.ShowView<ReorderItemsViewModel>(
             new ReorderItemsParameter("Порядок групп услуг", groups.Select(x => new ComboBoxItem(x.Id, x.Name)).ToList(), okCommand: x => ReorderGroupsHandleOkAsync(x, groups)),
             this);
        }

        private async Task<bool> ReorderGroupsHandleOkAsync(IReadOnlyCollection<ComboBoxItem> changedGroups, List<AdditionalServiceGroupViewItem> groups)
        {
            bool success = false;

            Dictionary<int, int> reorderedItems = changedGroups
               .Select((x, i) => new { x.Id, Position = i })
               .ToDictionary(x => x.Id, x => x.Position);

            try
            {
                IReadOnlyCollection<AdditionalServiceGroupPositionDto> positions = reorderedItems.Select(x => new AdditionalServiceGroupPositionDto(x.Key, x.Value)).ToList();

                await WebClient.ExecuteApiRequestAsync(new ReorderAdditionalServiceGroups(SelectedGroup.Id, SelectedGroup.ParentId, positions.ToArray()));

                bool isChanged = false;

                foreach (AdditionalServiceGroupViewItem group in groups)
                {
                    if (reorderedItems.TryGetValue(group.Id, out int position) && group.Position != position)
                    {
                        group.Position = position;

                        isChanged = true;
                    }
                }

                if (!isChanged)
                {
                    return true;
                }

                if (SelectedGroup.ParentId.HasValue)
                {
                    AdditionalServiceGroupViewItem groupToReorder = RecursiveFindGroup(AdditionalServiceGroups, SelectedGroup.ParentId.Value);
                    groupToReorder.Groups = groupToReorder.Groups.OrderBy(x => x.Position).ToObservableCollection();
                }
                else
                {
                    AdditionalServiceGroups = AdditionalServiceGroups.OrderBy(x => x.Position).ToObservableCollection();
                }

                RaisePropertyChanged(nameof(AdditionalServiceGroups));

                MessageFacadeService.ShowNotificationInfo("Позиции групп успешно изменены");

                success = true;
            }
            catch (UnexpectedSatusException exception)
            {
                MessageFacadeService.ShowNotificationError("Ошибка при изменении позиций групп");
                MessageFacadeService.ShowValidationResultView("Ошибки при изменении позиций групп", exception.GetErrorItems(), this);
            }
            catch (UnexpectedErrorException exception)
            {
                Logger.LogError(exception, "Failed to reorder additional service groups");
                MessageFacadeService.ShowValidationResultView(Resources.ServerConnectError, new[] { new ValidationResultItem(Resources.ServerConnectError, true) }, this);
            }
            catch (Exception exception)
            {
                MessageFacadeService.ShowNotificationError("Ошибка при изменении позиций групп");
                Logger.LogError(exception, "Error while reordering additional service groups");
            }

            return success;
        }

        private void ReorderPriority()
        {
            ReorderItemsViewModel viewModel = DialogDocumentManagerService.ShowView<ReorderItemsViewModel>(
              new ReorderItemsParameter("Приоритет", allServices.OrderBy(x => x.Priority).Select(x => new ComboBoxItem(x.Id, $"{x.Name} ({x.GroupName})")).ToList(), false, ReorderPriorityHandleOkAsync),
              this);
        }

        private async Task<bool> ReorderPriorityHandleOkAsync(IReadOnlyCollection<ComboBoxItem> changedPriorities)
        {
            bool success = false;

            Dictionary<int, int> reorderedItems = changedPriorities
              .Select((x, i) => new { x.Id, Priority = i })
              .ToDictionary(x => x.Id, x => x.Priority);

            try
            {
                IReadOnlyCollection<AdditionalServicePriorityDto> priorities = reorderedItems.Select(x => new AdditionalServicePriorityDto(x.Key, x.Value)).ToList();

                await WebClient.ExecuteApiRequestAsync(new ReorderAdditionalServicesPriority(priorities.ToArray()));

                bool isChanged = false;

                foreach (AdditionalServicesViewItem service in allServices)
                {
                    if (reorderedItems.TryGetValue(service.Id, out int priority) && service.Priority != priority)
                    {
                        service.Priority = priority;

                        isChanged = true;
                    }
                }

                if (!isChanged)
                {
                    return true;
                }

                MessageFacadeService.ShowNotificationInfo("Приоритеты услуг успешно изменены");

                success = true;
            }
            catch (UnexpectedSatusException exception)
            {
                MessageFacadeService.ShowNotificationError("Ошибка при изменении приоритета услуг");
                MessageFacadeService.ShowValidationResultView("Ошибки при изменении приоритета услуг", exception.GetErrorItems(), this);
            }
            catch (UnexpectedErrorException exception)
            {
                Logger.LogError(exception, "Failed to reorder priority additional services");
                MessageFacadeService.ShowValidationResultView(Resources.ServerConnectError, new[] { new ValidationResultItem(Resources.ServerConnectError, true) }, this);
            }
            catch (Exception exception)
            {
                MessageFacadeService.ShowNotificationError("Ошибка при изменении приоритета услуг");
                Logger.LogError(exception, "Error while reordering priority additional services");
            }

            return success;
        }

        private void OnAdditionalServiceGroupMessage(EntityMessage<AdditionalServiceGroupDto> message)
        {
            switch (message.MessageType)
            {
                case MessageType.Added:

                    AdditionalServiceGroupViewItem group = Mapper.Map<AdditionalServiceGroupViewItem>(message.Entity);

                    allGroups.Add(group);

                    if (group.ParentId == null)
                    {
                        AdditionalServiceGroups.Insert(0, group);
                    }
                    else
                    {
                        AdditionalServiceGroupViewItem parentGroup = RecursiveFindGroup(AdditionalServiceGroups, group.ParentId.Value);

                        parentGroup?.Groups.Add(group);
                    }

                    break;
                case MessageType.Changed:
                    AdditionalServiceGroupViewItem groupToEdit = RecursiveFindGroup(AdditionalServiceGroups, message.Entity.Id);

                    if (groupToEdit != null)
                    {
                        Mapper.Map(message.Entity, groupToEdit);
                    }

                    break;
            }

            RaisePropertyChanged(nameof(AdditionalServiceGroups));
        }

        private void OnAdditionalServiceMessage(EntityMessage<AdditionalServiceDto> message)
        {
            switch (message.MessageType)
            {
                case MessageType.Added:

                    AdditionalServicesViewItem additionalService = Mapper.Map<AdditionalServicesViewItem>(message.Entity);

                    allServices.Add(additionalService);

                    AdditionalServiceGroupViewItem parentGroup = RecursiveFindGroup(AdditionalServiceGroups, additionalService.GroupId);

                    parentGroup?.AdditionalServices.Add(additionalService);

                    break;
                case MessageType.Changed:
                    AdditionalServicesViewItem serviceToEdit = RecursiveFindService(AdditionalServiceGroups, message.Entity.Id);

                    if (serviceToEdit != null)
                    {
                        if (serviceToEdit.GroupId != message.Entity.GroupId)
                        {
                            AdditionalServiceGroupViewItem oldGroup = RecursiveFindGroup(AdditionalServiceGroups, serviceToEdit.GroupId);

                            oldGroup.AdditionalServices.Remove(serviceToEdit);

                            AdditionalServiceGroupViewItem newGroup = RecursiveFindGroup(AdditionalServiceGroups, message.Entity.GroupId);

                            newGroup.AdditionalServices.Add(serviceToEdit);
                        }

                        Mapper.Map(message.Entity, serviceToEdit);
                    }

                    break;
            }

            RaisePropertyChanged(nameof(AdditionalServiceGroups));
        }
    }
}