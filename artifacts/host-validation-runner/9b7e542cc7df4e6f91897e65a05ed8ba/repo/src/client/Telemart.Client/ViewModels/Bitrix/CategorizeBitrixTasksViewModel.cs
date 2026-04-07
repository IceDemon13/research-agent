using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using Microsoft.Extensions.Logging;
using Telemart.Client.Common.Services;
using Telemart.Client.Common.Utils;
using Telemart.Client.Core.Exceptions;
using Telemart.Client.Data.Requests.Features.Bitrix;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.Properties;
using Telemart.Client.TransferObjects.Bitrix;
using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.Bitrix
{
    public class CategorizeBitrixTasksViewModel : TelemartDialogViewModelBase
    {
        public CategorizeBitrixTasksViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            IMapper mapper)
            : base(webClient, dictionaries, messageFacadeService)
        {
            Mapper = mapper;

            Tasks = new ObservableRangeCollection<CategorizedBitrixTaskViewItem>();
        }

        public ReadOnlyObservableCollection<BitrixCategoryViewItem> Categories
        {
            get { return GetProperty(() => Categories); }
            private set { SetProperty(() => Categories, value); }
        }

        public ReadOnlyObservableCollection<BitrixTaskRole> Roles
        {
            get { return GetProperty(() => Roles); }
            private set { SetProperty(() => Roles, value); }
        }

        public ReadOnlyObservableCollection<Priority> Priorities
        {
            get { return GetProperty(() => Priorities); }
            private set { SetProperty(() => Priorities, value); }
        }

        public ObservableRangeCollection<CategorizedBitrixTaskViewItem> Tasks
        {
            get { return GetProperty(() => Tasks); }
            private set { SetProperty(() => Tasks, value); }
        }

        public CategorizedBitrixTaskViewItem SelectedTask
        {
            get { return GetProperty(() => SelectedTask); }
            set { SetProperty(() => SelectedTask, value, OnSelectedTaskChanged); }
        }

        private IMapper Mapper { get; }

        protected override async Task HandleLoadedAsync()
        {
            List<BitrixTaskDto> tasks = (List<BitrixTaskDto>)Parameter;

            Tasks.AddRange(tasks.Select(x => Mapper.Map<CategorizedBitrixTaskViewItem>(x)));

            Roles = Dictionaries.GetItems<BitrixTaskRole>().ToReadOnlyObservableCollection();
            Priorities = Dictionaries.GetItems<Priority>().ToReadOnlyObservableCollection();

            await RefreshCategories();

            Title = "Выберите категории";

            async Task RefreshCategories()
            {
                List<BitrixCategoryDto> categories = await WebClient.ExecuteApiRequestAsync(new QueryBitrixCategories());

                Categories = categories.Select(x => Mapper.Map<BitrixCategoryViewItem>(x)).ToReadOnlyObservableCollection();

                OnSelectedTaskChanged();

                Categories.ForEach(x => x.PropertyChanged += OnCategoryPropertyChanged);
            }
        }

        protected override async Task HandleOkAsync()
        {
            if (Tasks.Any(x => IDataErrorInfoHelper.HasErrors(x)))
            {
                return;
            }

            CategorizedBitrixTaskViewItem emptyCategoriesTask = Tasks.FirstOrDefault(x => !x.CategoryIds.Any());

            if (emptyCategoriesTask != null)
            {
                SelectedTask = emptyCategoriesTask;
                MessageFacadeService.ShowNotificationWarning("Назначьте категории всем задачам");
                return;
            }

            foreach (CategorizedBitrixTaskViewItem task in Tasks.Where(x => !x.Processed))
            {
                await ProcessItemAsync(task);
            }

            if (Tasks.All(x => x.IsSuccess))
            {
                IsOk = true;
                Close();
            }
        }

        protected override void Close()
        {
            Categories.ForEach(x => x.PropertyChanged -= OnCategoryPropertyChanged);

            base.Close();
        }

        private async Task ProcessItemAsync(CategorizedBitrixTaskViewItem item)
        {
            try
            {
                await WebClient.ExecuteApiRequestAsync(new CreateBitrixTask(item.BitrixId, item.CategoryIds.ToArray(), item.OurPriority.Id));
                item.Error = null;
                item.Processed = true;
            }
            catch (UnexpectedSatusException exception)
            {
                item.Error = string.Join(", ", exception.GetErrorItems().Select(x => x.Message));
            }
            catch (UnexpectedErrorException exception)
            {
                item.Error = Resources.ServerUnavailable;

                Logger.LogError(exception, "Failed to categorize tasks");
            }
            catch (Exception exception)
            {
                item.Error = "Ошибка при категоризации задач";
                Logger.LogError(exception, "Error while categorizing tasks");
            }
        }

        private void OnCategoryPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            SelectedTask.CategoryIds = Categories.Where(x => x.Checked != false).Select(x => x.Id).ToObservableCollection();
        }

        private void OnSelectedTaskChanged()
        {
            Categories.CheckItems(SelectedTask?.CategoryIds ?? (IReadOnlyCollection<int>)Array.Empty<int>());
        }
    }
}