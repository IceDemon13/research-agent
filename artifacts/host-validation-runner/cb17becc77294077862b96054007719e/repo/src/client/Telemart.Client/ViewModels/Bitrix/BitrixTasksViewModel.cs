using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Input;
using AutoMapper;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using DevExpress.Xpf.Grid;
using DevExpress.Xpf.Grid.DragDrop;
using Microsoft.Extensions.Logging;
using Telemart.Client.Common.Messages;
using Telemart.Client.Common.Services;
using Telemart.Client.Common.Utils;
using Telemart.Client.Core.Exceptions;
using Telemart.Client.Core.Helpers;
using Telemart.Client.Data.Requests.Features.Bitrix;
using Telemart.Client.Data.Requests.Features.Bitrix.Actions;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.Helpers;
using Telemart.Client.Properties;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.Bitrix;
using Telemart.Client.ViewModels.Base;
using Telemart.Client.ViewModels.Validation;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.ViewModels.Bitrix
{
    public class BitrixTasksViewModel : TelemartViewModelBase, ISupportHotkeys
    {
        public BitrixTasksViewModel(
               IWebClient webClient,
               IDictionaries dictionaries,
               IMessageFacadeService messageFacadeService,
               IMapper mapper,
               IMessenger messenger)
               : base(webClient, dictionaries, messageFacadeService)
        {
            Mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));

            CancelFilteringCommand = new DelegateCommand(CancelFiltering);
            EditCommand = new DelegateCommand(Edit, () => SelectedTask != null);
            RefreshCommand = new AsyncCommand(RefreshAsync);
            DropCommand = new AsyncCommand<GridDropEventArgs>(DropAsync);

            Filter = new BitrixTasksFilterViewModel(dictionaries);

            Tasks = new ObservableRangeCollection<CategorizedBitrixTaskViewItem>();

            TasksCollectionView = CollectionViewSource.GetDefaultView(Tasks);

            TasksCollectionView.Filter = x => x is CategorizedBitrixTaskViewItem item && (SelectedCategory == null || item.CategoryIds.Contains(SelectedCategory.Id));

            messenger.Register<BitrixTaskMessage>(this, OnBitrixTaskMessage);
        }

        public BitrixTasksViewModel()
        {
        }

        #region Commands

        public IDelegateCommand CancelFilteringCommand { get; }

        public IDelegateCommand EditCommand { get; }

        public IAsyncCommand RefreshCommand { get; }

        public IAsyncCommand DropCommand { get; }

        #endregion

        #region INPC

        public BitrixTasksFilterViewModel Filter { get; }

        public bool IsSearchPanelClosed
        {
            get { return GetProperty(() => IsSearchPanelClosed); }
            set { SetProperty(() => IsSearchPanelClosed, value); }
        }

        public ReadOnlyObservableCollection<Priority> Priorities
        {
            get { return GetProperty(() => Priorities); }
            private set { SetProperty(() => Priorities, value); }
        }

        public ReadOnlyObservableCollection<BitrixTaskRole> Roles
        {
            get { return GetProperty(() => Roles); }
            private set { SetProperty(() => Roles, value); }
        }

        public ObservableCollection<BitrixCategoryDto> Categories
        {
            get { return GetProperty(() => Categories); }
            private set { SetProperty(() => Categories, value); }
        }

        public BitrixCategoryDto SelectedCategory
        {
            get { return GetProperty(() => SelectedCategory); }
            set { SetProperty(() => SelectedCategory, value, TasksCollectionView.Refresh); }
        }

        public ObservableRangeCollection<CategorizedBitrixTaskViewItem> Tasks
        {
            get { return GetProperty(() => Tasks); }
            set { SetProperty(() => Tasks, value); }
        }

        public ICollectionView TasksCollectionView
        {
            get { return GetProperty(() => TasksCollectionView); }
            set { SetProperty(() => TasksCollectionView, value); }
        }

        public CategorizedBitrixTaskViewItem SelectedTask
        {
            get { return GetProperty(() => SelectedTask); }
            set { SetProperty(() => SelectedTask, value); }
        }

        public bool IsColumnChooserVisible
        {
            get { return GetProperty(() => IsColumnChooserVisible); }
            set { SetProperty(() => IsColumnChooserVisible, value); }
        }

        #endregion
        private IDocumentManagerService DialogDocumentManagerService => GetService<IDocumentManagerService>("DialogDocumentManagerService", ServiceSearchMode.PreferParents);

        private IDocumentManagerService SizeableDialogDocumentManagerService => GetService<IDocumentManagerService>("SizeableDialogDocumentManagerService", ServiceSearchMode.PreferParents);

        private IMapper Mapper { get; }

        public bool HandleHotkey(HotkeyMessage msg)
        {
            bool handled = false;

            if (msg.ModifierKeys == ModifierKeys.Alt)
            {
                switch (msg.Key)
                {
                    case Key.L:
                        IsSearchPanelClosed = !IsSearchPanelClosed;
                        handled = true;
                        break;
                }
            }
            else
            {
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
            }

            return handled;
        }

        protected override Task HandleLoadedAsync()
        {
            Priorities = Dictionaries.GetItems<Priority>().ToReadOnlyObservableCollection();
            Roles = Dictionaries.GetItems<BitrixTaskRole>().ToReadOnlyObservableCollection();

            IsSearchPanelClosed = false;
            CancelFiltering();

            return Task.CompletedTask;
        }

        private void CancelFiltering()
        {
            Filter.ResetFilterValues();
            RefreshCommand.Execute(null);
        }

        private void Edit()
        {
            DialogDocumentManagerService.ShowView<BitrixTaskViewModel>(new BitrixTaskParameter(SelectedTask.Id), this);
        }

        private async Task RefreshAsync()
        {
            try
            {
                Result<BitrixTasksDto> tasks = await WebClient.ExecuteApiRequestAsync(new QueryBitrixTasks(Filter.GetFilteringItem()));

                Tasks.Clear();

                Tasks.AddRange(tasks.Data.Tasks
                    .OrderByDescending(x => x.Position)
                    .Select(x => Mapper.Map<CategorizedBitrixTaskViewItem>(x)));

                await RefreshCategories(Tasks.SelectMany(x => x.CategoryIds).Distinct().ToArray());

                if (tasks.Data.NewTasks.Any() &&
                    MessageFacadeService.Confirm(GetConfirmMessage(tasks.Data.NewTasks.Count)))
                {
                    CategorizeBitrixTasksViewModel viewModel = SizeableDialogDocumentManagerService.ShowView<CategorizeBitrixTasksViewModel>(tasks.Data.NewTasks, this);

                    if (viewModel.IsOk)
                    {
                        await RefreshAsync();
                    }
                }
            }
            catch (UnexpectedSatusException exception)
            {
                MessageFacadeService.ShowNotificationError("Ошибка при загрузке задач из битрикса");
                ShowValidationResultView("Ошибки при загрузке задач из битрикса", exception.GetErrorItems());
            }
            catch (UnexpectedErrorException exception)
            {
                Logger.LogError(exception, "Failed to load tasks from bitrix24");
                ShowValidationResultView(Resources.ServerConnectError, new[] { new ValidationResultItem(Resources.ServerUnavailable, true) });
            }
            catch (Exception exception)
            {
                MessageFacadeService.ShowNotificationError("Ошибка загрузке задач из битрикса");
                Logger.LogError(exception, "Error while loading tasks from bitrix24");
            }

            string GetConfirmMessage(int tasksCount)
            {
                string appearedWord = WordEndingHelper.GetWordByNumber(tasksCount, "Появилась", "Появилось", "Появилось");
                string newWord = WordEndingHelper.GetWordByNumber(tasksCount, "новая", "новые", "новых");
                string taskWord = WordEndingHelper.GetWordByNumber(tasksCount, "задача", "задачи", "задач");

                return $"{appearedWord} {tasksCount} {newWord} {taskWord}, обработать?";
            }

            async Task RefreshCategories(int[] categoryIds)
            {
                int? selectedCategoryId = SelectedCategory?.Id;

                List<BitrixCategoryDto> categories = await WebClient.ExecuteApiRequestAsync(new QueryBitrixCategories());

                Categories = categories.Where(x => categoryIds.Contains(x.Id)).ToObservableCollection();

                if (selectedCategoryId.HasValue)
                {
                    SelectedCategory = categories.FirstOrDefault(x => x.Id == selectedCategoryId);
                }
            }
        }

        private async Task DropAsync(GridDropEventArgs args)
        {
            if (args.DraggedRows.Count == 1
                && args.DraggedRows[0] is CategorizedBitrixTaskViewItem draggedRow
                && args.TargetRow is CategorizedBitrixTaskViewItem targetRow)
            {
                int newPosition = args.DropTargetType == DropTargetType.InsertRowsBefore
                    ? targetRow.Position
                    : targetRow.Position - 1;

                newPosition = newPosition < draggedRow.Position ? newPosition + 1 : newPosition;

                if (newPosition != draggedRow.Position)
                {
                    try
                    {
                        Result<List<BitrixTaskPositionDto>> positions = await WebClient.ExecuteApiRequestAsync(new MoveBitrixTask(draggedRow.Id, newPosition));

                        if (positions.Data.Any())
                        {
                            ChangePositions(positions.Data);
                        }
                    }
                    catch (UnexpectedSatusException exception)
                    {
                        MessageFacadeService.ShowNotificationError("Ошибка при перемещении задач");
                        ShowValidationResultView("Ошибки при перемещении задач", exception.GetErrorItems());
                    }
                    catch (UnexpectedErrorException exception)
                    {
                        Logger.LogError(exception, "Failed to move tasks from bitrix24");
                        ShowValidationResultView(Resources.ServerConnectError, new[] { new ValidationResultItem(Resources.ServerUnavailable, true) });
                    }
                    catch (Exception exception)
                    {
                        MessageFacadeService.ShowNotificationError("Ошибка при перемещении задач");
                        Logger.LogError(exception, "Error while moving tasks from bitrix24");
                    }
                }
            }
        }

        private void ChangePositions(List<BitrixTaskPositionDto> positions)
        {
            Dictionary<int, int> positionsDictionary = positions.ToDictionary(x => x.Id, x => x.Position);

            foreach (CategorizedBitrixTaskViewItem task in Tasks)
            {
                if (positionsDictionary.TryGetValue(task.Id, out int position))
                {
                    task.Position = position;
                }
            }
        }

        private bool ShowValidationResultView(string title, IReadOnlyCollection<ValidationResultItem> validationItems)
        {
            ValidationResultViewModel viewModel = SizeableDialogDocumentManagerService.ShowView<ValidationResultViewModel>(
                new ValidationResultViewModelParameter(title, validationItems),
                this);

            return viewModel.IsOk;
        }

        private void OnBitrixTaskMessage(BitrixTaskMessage message)
        {
            CategorizedBitrixTaskViewItem newViewItem = Mapper.Map<CategorizedBitrixTaskViewItem>(message.Entity);

            switch (message.MessageType)
            {
                case MessageType.Added:
                    Tasks.Insert(0, newViewItem);

                    break;
                case MessageType.Changed:
                    Tasks.DoActionWithItem(x => x.Id == message.Entity.Id, viewItem => Mapper.Map(message.Entity, viewItem));

                    break;
            }

            Tasks.SortDescending(x => x.Position);
        }
    }
}