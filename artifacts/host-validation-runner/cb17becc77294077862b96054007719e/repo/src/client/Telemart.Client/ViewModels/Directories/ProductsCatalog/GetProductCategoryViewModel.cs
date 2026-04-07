using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using DevExpress.Mvvm.Native;
using Telemart.Client.Common.Services;
using Telemart.Client.Data.Requests.Features.Category;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.TransferObjects;
using Telemart.Client.ViewModels.Base;
using Telemart.Client.ViewModels.Directories.Category;

namespace Telemart.Client.ViewModels.Directories.ProductsCatalog
{
    public class GetProductCategoryViewModel : TelemartDialogViewModelBase
    {
        public GetProductCategoryViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            IMapper mapper)
            : base(webClient, dictionaries, messageFacadeService)
        {
            Mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        }

        #region INPC

        public ReadOnlyObservableCollection<CategoryProductCatalogViewItem> Categories
        {
            get { return GetProperty(() => Categories); }
            private set { SetProperty(() => Categories, value); }
        }

        public CategoryProductCatalogViewItem SelectedCategory
        {
            get { return GetProperty(() => SelectedCategory); }
            set { SetProperty(() => SelectedCategory, value); }
        }

        #endregion

        private IMapper Mapper { get; }

        protected override async Task HandleLoadedAsync()
        {
            GetProductCategoryParameter parameter = (GetProductCategoryParameter)Parameter;

            List<CategoryFullDto> categories = await WebClient.ExecuteApiRequestAsync(new QueryCategoriesFull(), true);
            CategoryFullDto rootCategory = categories.FirstOrDefault(c => c.Id == parameter.RootCategoryId);

            if (rootCategory == null)
            {
                MessageFacadeService.ShowNotificationError("Категория не найдена");
                Close();

                return;
            }

            Categories = categories.Where(c => c.Left >= rootCategory.Left && c.Right <= rootCategory.Right && (c.Active > 0 || c.Id == parameter.SelectedCategoryId))
                .OrderBy(c => c.Left)
                .Select(Mapper.Map<CategoryProductCatalogViewItem>).ToReadOnlyObservableCollection();

            SelectedCategory = Categories.FirstOrDefault(c => c.Id == parameter.SelectedCategoryId);

            Title = "Выбор категории";
        }

        protected override Task HandleOkAsync()
        {
            IsOk = true;
            Close();
            return Task.CompletedTask;
        }
    }
}
