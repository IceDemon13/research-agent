using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using DevExpress.Mvvm.Native;
using DevExpress.Xpf.Grid;
using Telemart.Client.Common.Services;
using Telemart.Client.Data.Requests.Features.Category;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.Paging;
using Telemart.Client.ViewModels.Base;
using Telemart.Client.ViewModels.Directories.Category;

namespace Telemart.Client.ViewModels.Common
{
    public sealed class ChooseCategoryViewModel : TelemartDialogViewModelBase
    {
        private IReadOnlyCollection<int> allowedCategoryIds;
        private string allowedCategoriesErrorMessage;

        public ChooseCategoryViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            IMapper mapper)
            : base(webClient, dictionaries, messageFacadeService)
        {
            Mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            SelectedCategories = new ObservableCollection<CategoryViewItem>();
        }

        public ChooseCategoryViewModel()
        {
        }

        #region INPC

        public ObservableCollection<CategoryViewItem> Categories
        {
            get { return GetProperty(() => Categories); }
            private set { SetProperty(() => Categories, value); }
        }

        public CategoryViewItem SelectedCategory
        {
            get { return GetProperty(() => SelectedCategory); }
            set { SetProperty(() => SelectedCategory, value); }
        }

        public ObservableCollection<CategoryViewItem> SelectedCategories
        {
            get { return GetProperty(() => SelectedCategories); }
            set { SetProperty(() => SelectedCategories, value); }
        }

        public MultiSelectMode SelectMode
        {
            get { return GetProperty(() => SelectMode); }
            set { SetProperty(() => SelectMode, value); }
        }

        public string Label
        {
            get { return GetProperty(() => Label); }
            private set { SetProperty(() => Label, value); }
        }

        public bool LabelVisible
        {
            get { return GetProperty(() => LabelVisible); }
            private set { SetProperty(() => LabelVisible, value); }
        }

        #endregion

        private IMapper Mapper { get; }

        protected override async Task HandleLoadedAsync()
        {
            ChooseCategoryParameter parameter = (ChooseCategoryParameter)Parameter;

            SelectMode = parameter.MultiSelect ? MultiSelectMode.MultipleRow : MultiSelectMode.Row;

            if (parameter.MultiSelect)
            {
                Label = "Можно выбрать несколько категорий 💡";
                LabelVisible = true;
            }

            PagedResult<CategoryDto> categories = await WebClient.ExecuteApiRequestAsync(new QueryCategories(), true);

            Categories = categories.Data
                .Select(x => Mapper.Map<CategoryViewItem>(x))
                .ToObservableCollection();

            allowedCategoryIds = parameter.AllowedCategoryIds;
            allowedCategoriesErrorMessage = parameter.AllowedCategoriesErrorMessage;

            Title = "Выберите категорию";
        }

        protected override Task HandleOkAsync()
        {
            if (SelectMode == MultiSelectMode.Row)
            {
                if (SelectedCategory != null)
                {
                    if (allowedCategoryIds?.Any() == true && !allowedCategoryIds.Contains(SelectedCategory.Id))
                    {
                        MessageFacadeService.ShowNotificationError(allowedCategoriesErrorMessage);
                        return Task.CompletedTask;
                    }

                    IsOk = true;
                    Close();
                }
                else
                {
                    MessageFacadeService.ShowNotificationWarning("Выберите категорию");
                    return Task.CompletedTask;
                }
            }

            if (SelectMode == MultiSelectMode.MultipleRow)
            {
                if (SelectedCategories?.Any() == true)
                {
                    if (allowedCategoryIds?.Any() == true && SelectedCategories.Any(x => !allowedCategoryIds.Contains(x.Id)))
                    {
                        MessageFacadeService.ShowNotificationError(allowedCategoriesErrorMessage);
                        return Task.CompletedTask;
                    }

                    IsOk = true;
                    Close();
                }
                else
                {
                    MessageFacadeService.ShowNotificationWarning("Выберите категории");
                    return Task.CompletedTask;
                }
            }

            return Task.CompletedTask;
        }
    }
}