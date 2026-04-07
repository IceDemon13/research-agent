using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using Telemart.Client.Common.ErrorHandler;
using Telemart.Client.Common.MvvmEnhancements;
using Telemart.Client.Common.Services;
using Telemart.Client.Data.Requests.Features.Category;
using Telemart.Client.Data.Requests.Features.InvoiceBudget;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Helpers;
using Telemart.Client.TransferObjects.InvoiceBudget;
using Telemart.Client.TransferObjects.Paging;
using Telemart.Client.ViewModels.Base;
using Telemart.Common.ErrorHandling;
using CategoryDto = Telemart.Client.TransferObjects.CategoryDto;

namespace Telemart.Client.ViewModels.Store.Invoice
{
    public sealed class InvoiceEditCategoryBudgetsViewModel : TelemartDialogViewModelBase
    {
        private readonly IMapper _mapper;
        private readonly IErrorHandler _errorHandler;
        private List<InvoiceCategoryBudgetViewItem> _allCategoryBudgets;
        private IReadOnlyCollection<CategoryDto> _allCategories;

        public InvoiceEditCategoryBudgetsViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            IMapper mapper,
            IErrorHandler errorHandler)
            : base(webClient, dictionaries, messageFacadeService)
        {
            _mapper = mapper;
            _errorHandler = errorHandler;

            RemoveCommand = new DelegateCommand(Remove);
        }

        public IDelegateCommand RemoveCommand { get; }

        public ObservableCollection<InvoiceCategoryBudgetViewItem> CurrentBudgetCategories
        {
            get { return GetProperty(() => CurrentBudgetCategories); }
            set { SetProperty(() => CurrentBudgetCategories, value); }
        }

        public ReadOnlyObservableCollection<ComboBoxItem> CategoryNames
        {
            get { return GetProperty(() => CategoryNames); }
            set { SetProperty(() => CategoryNames, value); }
        }

        public ObservableCollection<InvoiceBudgetViewItem> Budgets
        {
            get { return GetProperty(() => Budgets); }
            set { SetProperty(() => Budgets, value); }
        }

        public InvoiceBudgetViewItem SelectedBudget
        {
            get { return GetProperty(() => SelectedBudget); }
            set { SetProperty(() => SelectedBudget, value, OnSelectedBudgetChanged); }
        }

        public InvoiceCategoryBudgetViewItem SelectedCategoryBudget
        {
            get { return GetProperty(() => SelectedCategoryBudget); }
            set { SetProperty(() => SelectedCategoryBudget, value); }
        }

        public bool IsLongOperationInProgress
        {
            get { return GetProperty(() => IsLongOperationInProgress); }
            private set { SetProperty(() => IsLongOperationInProgress, value); }
        }

        protected override async Task HandleLoadedAsync()
        {
            Task<List<InvoiceCategoryBudgetDto>> allCategoryBudgetsTask = WebClient.ExecuteApiRequestAsync(new QueryInvoiceCategoryBudgets());

            Task<List<InvoiceBudgetDto>> budgetsTask = WebClient.ExecuteApiRequestAsync(new QueryInvoiceBudgets());

            Task<PagedResult<CategoryDto>> categoriesTask = WebClient.ExecuteApiRequestAsync(new QueryCategories(), true);

            (List<InvoiceCategoryBudgetDto> InvoiceCategoryBudgets, List<InvoiceBudgetDto> InvoiceBudgets, PagedResult<CategoryDto> Categories) result = await TaskExt
                .WhenAll(allCategoryBudgetsTask, budgetsTask, categoriesTask);

            Budgets = result.InvoiceBudgets.Select(x => _mapper.Map<InvoiceBudgetViewItem>(x)).ToObservableCollection();

            _allCategoryBudgets = result.InvoiceCategoryBudgets.Select(x => _mapper.Map<InvoiceCategoryBudgetViewItem>(x)).ToList();
            _allCategories = result.Categories.Data.Where(x => x.Active > 0).ToArray();

            CategoryNames = result.Categories.Data.Select(x => new ComboBoxItem(x.Id, x.Name)).ToReadOnlyObservableCollection();

            SelectedBudget = Budgets.FirstOrDefault(x => x.DateFrom <= DateTime.Now.Date && x.DateTo >= DateTime.Now.Date);

            if (SelectedBudget is null)
            {
                if (SelectedBudget is null)
                {
                    SelectedBudget = Budgets.MaxBy(x => x.DateFrom);
                }
            }

            await base.HandleLoadedAsync();

            Title = "Бюджеты по категориям";
        }

        protected override async Task HandleOkAsync()
        {
            if (_allCategoryBudgets.Any(x => x.Budget is <= 0 or >= 1_000_000_000))
            {
                MessageFacadeService.ShowNotificationError("Бюджет должен быть в диапазоне от 1 до 1 000 000 000");
                return;
            }

            AddCurrentCategoryBudgetsToSaveCollection();

            InvoiceCategoryBudgetDto[] budgetsToSave = _allCategoryBudgets
                .Where(x => x.Budget > 0)
                .Select(x => _mapper.Map<InvoiceCategoryBudgetDto>(x))
                .ToArray();

            if (!MessageFacadeService.Confirm("Вы уверены?"))
            {
                return;
            }

            SaveInvoiceCategoryBudgetsDto saveInvoiceBudgetsDto = new SaveInvoiceCategoryBudgetsDto(budgetsToSave);

            Result result = await _errorHandler.HandleErrorsAsync(
                _ => WebClient.ExecuteApiRequestAsync(new SaveInvoiceCategoryBudgets(saveInvoiceBudgetsDto)),
                "сохранении бюджетов категорий",
                "Бюджеты сохранены",
                this,
                true);

            if (result?.IsSuccess == true)
            {
                CloseOk();
            }
        }

        private void OnSelectedBudgetChanged()
        {
            if (SelectedBudget is not null)
            {
                IsLongOperationInProgress = true;

                AddCurrentCategoryBudgetsToSaveCollection();

                CurrentBudgetCategories = _allCategoryBudgets.Where(x => x.InvoiceBudgetId == SelectedBudget.Id).ToObservableCollection();

                foreach (CategoryDto category in _allCategories)
                {
                    if (CurrentBudgetCategories.All(x => x.CategoryId != category.Id))
                    {
                        CurrentBudgetCategories.Add(new InvoiceCategoryBudgetViewItem
                        {
                            CategoryId = category.Id,
                            ParentCategoryId = category.ParentId,
                            Budget = null,
                            InvoiceBudgetId = SelectedBudget.Id,
                            CategoryPosition = category.Position
                        });
                    }
                }

                CurrentBudgetCategories = CurrentBudgetCategories.OrderBy(x => x.CategoryPosition).ToObservableCollection();

                IsLongOperationInProgress = false;
            }
        }

        private void Remove()
        {
            foreach (InvoiceCategoryBudgetViewItem currentBudgetCategory in CurrentBudgetCategories)
            {
                currentBudgetCategory.Budget = null;
            }
        }

        private void AddCurrentCategoryBudgetsToSaveCollection()
        {
            if (CurrentBudgetCategories is not null)
            {
                foreach (InvoiceCategoryBudgetViewItem currentBudgetCategory in CurrentBudgetCategories)
                {
                    InvoiceCategoryBudgetViewItem budgetCategory = _allCategoryBudgets.FirstOrDefault(x => x.CategoryId == currentBudgetCategory.CategoryId && x.InvoiceBudgetId == currentBudgetCategory.InvoiceBudgetId);

                    if (budgetCategory is null)
                    {
                        _allCategoryBudgets.Add(currentBudgetCategory);
                    }
                    else
                    {
                        budgetCategory.Budget = currentBudgetCategory.Budget;
                    }
                }
            }
        }
    }
}