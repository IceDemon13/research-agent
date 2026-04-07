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
using Telemart.Client.Data.Requests.Features.AssemblyTest;
using Telemart.Client.Data.Requests.Features.AssemblyTest.Actions;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.Properties;
using Telemart.Client.TransferObjects;
using Telemart.Client.ViewModels.Base;
using Telemart.Client.ViewModels.Common;
using Telemart.Client.ViewModels.Validation;

namespace Telemart.Client.ViewModels.AssemblyTest
{
    public sealed class AssemblyTestsViewModel : TelemartViewModelBase, ISupportHotkeys
    {
        private List<AssemblyTestsViewItem> allTests;
        private List<AssemblyTestGroupViewItem> allGroups;

        public AssemblyTestsViewModel(
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
            AddCommand = new DelegateCommand(Add, () => SelectedElement is AssemblyTestGroupViewItem item && item.Groups?.Any() != true);
            AddGroupCommand = new DelegateCommand(AddGroup, () => (SelectedElement is AssemblyTestGroupViewItem item && item.Tests?.Any() != true) || SelectedElement == null);
            ReorderCommand = new DelegateCommand(Reorder, () => SelectedGroup != null || SelectedTest != null);
            MoveCommand = new DelegateCommand(Move, () => SelectedGroup != null);

            Messenger.Register<EntityMessage<AssemblyTestGroupDto>>(this, OnTestGroupMessage);
            Messenger.Register<EntityMessage<AssemblyTestDto>>(this, OnTestMessage);

            allTests = new List<AssemblyTestsViewItem>();
            allGroups = new List<AssemblyTestGroupViewItem>();
        }

        public AssemblyTestsViewModel()
        {
        }

        #region Commands

        public IDelegateCommand ReorderCommand { get; }

        public IAsyncCommand RefreshCommand { get; }

        public IDelegateCommand EditCommand { get; }

        public IDelegateCommand AddCommand { get; }

        public IDelegateCommand AddGroupCommand { get; }

        public IDelegateCommand MoveCommand { get; }

        #endregion

        public ObservableCollection<AssemblyTestGroupViewItem> TestGroups
        {
            get { return GetProperty(() => TestGroups); }
            set { SetProperty(() => TestGroups, value); }
        }

        public BindableBase SelectedElement
        {
            get { return GetProperty(() => SelectedElement); }
            set { SetProperty(() => SelectedElement, value, () => RaisePropertiesChanged(nameof(SelectedGroup), nameof(SelectedTest))); }
        }

        public bool IsColumnChooserVisible
        {
            get { return GetProperty(() => IsColumnChooserVisible); }
            set { SetProperty(() => IsColumnChooserVisible, value); }
        }

        public AssemblyTestGroupViewItem SelectedGroup => SelectedElement is AssemblyTestGroupViewItem ? (AssemblyTestGroupViewItem)SelectedElement : null;

        public AssemblyTestsViewItem SelectedTest => SelectedElement is AssemblyTestsViewItem ? (AssemblyTestsViewItem)SelectedElement : null;

        private IMapper Mapper { get; }

        private IMessenger Messenger { get; }

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
            RefreshCommand.Execute(null);
            return base.HandleLoadedAsync();
        }

        protected override void OnInitializeInDesignMode()
        {
            int id = 1;

            TestGroups = new ObservableCollection<AssemblyTestGroupViewItem>
            {
                new AssemblyTestGroupViewItem
                {
                    Id = id++,
                    Name = "General",
                    NameUa = "GeneralUa",
                    Groups = new ObservableCollection<AssemblyTestGroupViewItem>
                    {
                        new AssemblyTestGroupViewItem
                        {
                            Id = id++,
                            Name = "Group1",
                            NameUa = "Group1Ua",
                            Groups = new ObservableCollection<AssemblyTestGroupViewItem>
                            {
                                new AssemblyTestGroupViewItem
                                {
                                    Id = id++,
                                    Name = "Group5",
                                    NameUa = "Group5Ua",
                                    EmployeeLockId = 1,
                                    Groups = new ObservableCollection<AssemblyTestGroupViewItem>
                                    {
                                    }
                                },
                                new AssemblyTestGroupViewItem
                                {
                                    Id = id++,
                                    Name = "Group6",
                                    NameUa = "Group6Ua"
                                }
                            }
                        },
                        new AssemblyTestGroupViewItem
                        {
                            Id = id++,
                            Name = "Group2",
                            NameUa = "Group2Ua",
                            EmployeeLockId = 1
                        }
                    }
                },
                new AssemblyTestGroupViewItem
                {
                    Id = id++,
                    Name = "General2",
                    NameUa = "General2Ua",
                    Groups = new ObservableCollection<AssemblyTestGroupViewItem>
                    {
                        new AssemblyTestGroupViewItem
                        {
                            Id = id++,
                            Name = "Group3",
                            NameUa = "Group3Ua",
                            Tests = new ObservableCollection<AssemblyTestsViewItem>
                            {
                                new AssemblyTestsViewItem
                                {
                                    Id = id++,
                                    Name = "Test1",
                                    Description = "Description1",
                                    Active = true,
                                    AvailOnWeb = false,
                                    Position = 1,
                                    Required = true
                                },
                                new AssemblyTestsViewItem
                                {
                                    Id = id++,
                                    Name = "Test2",
                                    Description = "Description2",
                                    Active = false,
                                    AvailOnWeb = true,
                                    Position = 2,
                                    Required = true,
                                    EmployeeLockId = 1
                                },
                                new AssemblyTestsViewItem
                                {
                                    Id = id++,
                                    Name = "Test3",
                                    Description = "Description3",
                                    Active = true,
                                    AvailOnWeb = false,
                                    Position = 3,
                                    Required = false
                                }
                            }
                        },
                        new AssemblyTestGroupViewItem
                        {
                            Id = id++,
                            Name = "Group4",
                            NameUa = "Group4Ua"
                        }
                    }
                }
            };

            base.OnInitializeInDesignMode();
        }

        private static IEnumerable<AssemblyTestGroupViewItem> GetParentGroups(IEnumerable<AssemblyTestGroupViewItem> groups, int? parentId)
        {
            foreach (AssemblyTestGroupViewItem group in groups.Where(x => x.ParentId == parentId))
            {
                group.Groups = GetParentGroups(groups, group.Id).OrderBy(x => x.Position).ToObservableCollection();
                group.Tests = group.Tests.OrderBy(x => x.Position).ToObservableCollection();

                yield return group;
            }
        }

        private async Task RefreshAsync()
        {
            try
            {
                List<AssemblyTestGroupDto> assemblyTestGroups = await WebClient.ExecuteApiRequestAsync(new QueryAssemblyTestGroups());

                allGroups = assemblyTestGroups
                     .Select(x => Mapper.Map<AssemblyTestGroupViewItem>(x))
                     .ToList();

                allTests = allGroups.SelectMany(x => x.Tests).ToList();

                SelectedElement = null;

                TestGroups = GetParentGroups(allGroups, null).ToObservableCollection();
                TestGroups = TestGroups.OrderBy(x => x.Position).ToObservableCollection();
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Refresh assembly test view model");
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

            DialogDocumentManagerService
                .ShowView<AssemblyTestViewModel>(new AssemblyTestParameter(0, SelectedGroup.Id, SelectedGroup.Name), this);
        }

        private void AddGroup()
        {
            DialogDocumentManagerService.ShowView<AssemblyTestGroupViewModel>(new AssemblyTestGroupParameter(0, SelectedGroup?.Id, SelectedGroup?.Name), this);
        }

        private void Edit()
        {
            if (SelectedTest != null)
            {
                DialogDocumentManagerService.ShowView<AssemblyTestViewModel>(new AssemblyTestParameter(SelectedTest.Id, null, null), this);
                return;
            }

            if (SelectedGroup != null)
            {
                DialogDocumentManagerService.ShowView<AssemblyTestGroupViewModel>(new AssemblyTestGroupParameter(SelectedGroup.Id, SelectedGroup?.ParentId, SelectedGroup?.Name), this);
            }
        }

        private void Move()
        {
            AssemblyTestGroupMoveViewModel viewModel = DialogDocumentManagerService.ShowView<AssemblyTestGroupMoveViewModel>(
                new AssemblyTestGroupMoveParameter(SelectedGroup.Id),
                this);

            if (viewModel.IsOk)
            {
                RefreshCommand.Execute(null);
            }
        }

        private void Reorder()
        {
            if (SelectedTest != null)
            {
                ReorderTests();
            }

            if (SelectedGroup != null)
            {
                ReorderGroups();
            }
        }

        private void ReorderTests()
        {
            List<AssemblyTestsViewItem> tests = allTests
             .Where(x => x.GroupId == SelectedTest.GroupId)
             .OrderBy(x => x.Position)
             .ToList();

            ReorderItemsViewModel viewModel = DialogDocumentManagerService.ShowView<ReorderItemsViewModel>(
              new ReorderItemsParameter("Порядок тестов", tests.Select(x => new ComboBoxItem(x.Id, x.Name)).ToList(), okCommand: x => ReorderTestsHandleOkAsync(x, tests)),
              this);
        }

        private async Task<bool> ReorderTestsHandleOkAsync(IReadOnlyCollection<ComboBoxItem> changedTests, List<AssemblyTestsViewItem> tests)
        {
            bool success = false;

            Dictionary<int, int> reorderedItems = changedTests
               .Select((x, i) => new { x.Id, Position = i })
               .ToDictionary(x => x.Id, x => x.Position);

            try
            {
                IReadOnlyCollection<AssemblyTestPositionDto> positions = reorderedItems.Select(x => new AssemblyTestPositionDto(x.Key, x.Value)).ToList();

                await WebClient.ExecuteApiRequestAsync(new ReorderAssemblyTests(SelectedTest.GroupId, positions.ToArray()));

                bool isChanged = false;

                foreach (AssemblyTestsViewItem test in tests)
                {
                    if (reorderedItems.TryGetValue(test.Id, out int position) && test.Position != position)
                    {
                        test.Position = position;

                        isChanged = true;
                    }
                }

                if (!isChanged)
                {
                    return true;
                }

                AssemblyTestGroupViewItem groupToReorder = RecursiveFindGroup(TestGroups, SelectedTest.GroupId);
                groupToReorder.Tests = groupToReorder.Tests.OrderBy(x => x.Position).ToObservableCollection();

                RaisePropertyChanged(nameof(TestGroups));

                MessageFacadeService.ShowNotificationInfo("Позиции тестов успешно изменены");

                success = true;
            }
            catch (UnexpectedSatusException exception)
            {
                MessageFacadeService.ShowNotificationError("Ошибка при изменении позиций тестов");
                MessageFacadeService.ShowValidationResultView("Ошибки при изменении позиций тестов", exception.GetErrorItems(), this);
            }
            catch (UnexpectedErrorException exception)
            {
                Logger.LogError(exception, "Failed to reorder tests");
                MessageFacadeService.ShowValidationResultView(Resources.ServerConnectError, new[] { new ValidationResultItem(Resources.ServerConnectError, true) }, this);
            }
            catch (Exception exception)
            {
                MessageFacadeService.ShowNotificationError("Ошибка при изменении позиций тестов");
                Logger.LogError(exception, "Error while reordering tests");
            }

            return success;
        }

        private void ReorderGroups()
        {
            List<AssemblyTestGroupViewItem> groups = allGroups
                .Where(x => x.ParentId == SelectedGroup.ParentId)
                .OrderBy(x => x.Position)
                .ToList();

            ReorderItemsViewModel viewModel = DialogDocumentManagerService.ShowView<ReorderItemsViewModel>(
             new ReorderItemsParameter("Порядок групп тестов", groups.Select(x => new ComboBoxItem(x.Id, x.Name)).ToList(), okCommand: x => ReorderGroupsHandleOkAsync(x, groups)),
             this);
        }

        private async Task<bool> ReorderGroupsHandleOkAsync(IReadOnlyCollection<ComboBoxItem> changedGroups, List<AssemblyTestGroupViewItem> groups)
        {
            bool success = false;

            Dictionary<int, int> reorderedItems = changedGroups
               .Select((x, i) => new { x.Id, Position = i })
               .ToDictionary(x => x.Id, x => x.Position);

            try
            {
                IReadOnlyCollection<AssemblyTestGroupPositionDto> positions = reorderedItems.Select(x => new AssemblyTestGroupPositionDto(x.Key, x.Value)).ToList();

                await WebClient.ExecuteApiRequestAsync(new ReorderAssemblyTestGroups(SelectedGroup.Id, SelectedGroup.ParentId, positions.ToArray()));

                bool isChanged = false;

                foreach (AssemblyTestGroupViewItem group in groups)
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
                    AssemblyTestGroupViewItem groupToReorder = RecursiveFindGroup(TestGroups, SelectedGroup.ParentId.Value);
                    groupToReorder.Groups = groupToReorder.Groups.OrderBy(x => x.Position).ToObservableCollection();
                }
                else
                {
                    TestGroups = TestGroups.OrderBy(x => x.Position).ToObservableCollection();
                }

                RaisePropertyChanged(nameof(TestGroups));

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
                Logger.LogError(exception, "Failed to reorder test groups");
                MessageFacadeService.ShowValidationResultView(Resources.ServerConnectError, new[] { new ValidationResultItem(Resources.ServerConnectError, true) }, this);
            }
            catch (Exception exception)
            {
                MessageFacadeService.ShowNotificationError("Ошибка при изменении позиций групп");
                Logger.LogError(exception, "Error while reordering assembly groups");
            }

            return success;
        }

        private void OnTestGroupMessage(EntityMessage<AssemblyTestGroupDto> message)
        {
            switch (message.MessageType)
            {
                case MessageType.Added:

                    AssemblyTestGroupViewItem group = Mapper.Map<AssemblyTestGroupViewItem>(message.Entity);

                    allGroups.Add(group);

                    if (group.ParentId == null)
                    {
                        TestGroups.Insert(0, group);
                    }
                    else
                    {
                        AssemblyTestGroupViewItem parentGroup = RecursiveFindGroup(TestGroups, group.ParentId.Value);

                        parentGroup?.Groups.Add(group);
                    }

                    break;
                case MessageType.Changed:
                    AssemblyTestGroupViewItem groupToEdit = RecursiveFindGroup(TestGroups, message.Entity.Id);

                    if (groupToEdit != null)
                    {
                        Mapper.Map(message.Entity, groupToEdit);
                    }

                    break;
            }

            RaisePropertyChanged(nameof(TestGroups));
        }

        private void OnTestMessage(EntityMessage<AssemblyTestDto> message)
        {
            switch (message.MessageType)
            {
                case MessageType.Added:

                    AssemblyTestsViewItem test = Mapper.Map<AssemblyTestsViewItem>(message.Entity);

                    allTests.Add(test);

                    AssemblyTestGroupViewItem parentGroup = RecursiveFindGroup(TestGroups, test.GroupId);

                    parentGroup?.Tests.Add(test);

                    break;
                case MessageType.Changed:
                    AssemblyTestsViewItem testToEdit = RecursiveFindTest(TestGroups, message.Entity.Id);

                    if (testToEdit != null)
                    {
                        Mapper.Map(message.Entity, testToEdit);
                    }

                    break;
            }

            RaisePropertyChanged(nameof(TestGroups));
        }

        private AssemblyTestGroupViewItem RecursiveFindGroup(IEnumerable<AssemblyTestGroupViewItem> groups, int id)
        {
            foreach (AssemblyTestGroupViewItem group in groups)
            {
                if (group.Id == id)
                {
                    return group;
                }

                AssemblyTestGroupViewItem childGroup = RecursiveFindGroup(group.Groups, id);

                if (childGroup != null)
                {
                    return childGroup;
                }
            }

            return null;
        }

        private AssemblyTestsViewItem RecursiveFindTest(IEnumerable<AssemblyTestGroupViewItem> groups, int id)
        {
            foreach (AssemblyTestGroupViewItem group in groups)
            {
                AssemblyTestsViewItem test = group.Tests.FirstOrDefault(x => x.Id == id);

                if (test != null)
                {
                    return test;
                }

                AssemblyTestsViewItem childTest = RecursiveFindTest(group.Groups, id);

                if (childTest != null)
                {
                    return childTest;
                }
            }

            return null;
        }
    }
}