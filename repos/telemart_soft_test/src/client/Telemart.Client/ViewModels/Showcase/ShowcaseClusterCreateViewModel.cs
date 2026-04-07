using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using DevExpress.Mvvm.DataAnnotations;
using DevExpress.Mvvm.Native;
using Telemart.Client.Common.MvvmEnhancements;
using Telemart.Client.Common.Services;
using Telemart.Client.Data.Requests.Features.Category;
using Telemart.Client.Data.Requests.Features.Locations;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Properties;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.Locations;
using Telemart.Client.TransferObjects.Paging;
using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.Showcase
{
    public sealed class ShowcaseClusterCreateViewModel : TelemartDialogViewModelBase
    {
        public ShowcaseClusterCreateViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messagefacadeService)
            : base(webClient, dictionaries, messagefacadeService)
        {
        }

        public CategoryDto SelectedCategory
        {
            get { return GetProperty(() => SelectedCategory); }
            set { SetProperty(() => SelectedCategory, value); }
        }

        public ComboBoxItem? SelectedCluster
        {
            get { return GetProperty(() => SelectedCluster); }
            set { SetProperty(() => SelectedCluster, value); }
        }

        public int? Quantity
        {
            get { return GetProperty(() => Quantity); }
            set { SetProperty(() => Quantity, value); }
        }

        public ReadOnlyObservableCollection<CategoryDto> Categories
        {
            get { return GetProperty(() => Categories); }
            private set { SetProperty(() => Categories, value); }
        }

        public ReadOnlyObservableCollection<ComboBoxItem> Clusters
        {
            get { return GetProperty(() => Clusters); }
            private set { SetProperty(() => Clusters, value); }
        }

        public static void BuildMetadata(MetadataBuilder<ShowcaseClusterCreateViewModel> builder)
        {
            builder.Property(x => x.SelectedCategory)
                .Required(() => Resources.RequiredErrorMessage)
                .MatchesRule(x => x is null || x.IsParent, () => "Категория должна быть родительской");
            builder.Property(x => x.SelectedCluster)
                .Required(() => Resources.RequiredErrorMessage);
            builder.Property(x => x.Quantity)
                .MatchesRule(x => x > 0, () => "Значение должно быть больше 0");
        }

        protected override async Task HandleLoadedAsync()
        {
            await Task.WhenAll(RefreshCategoriesAsync(), RefreshClustersAsync());

            ShowcaseClusterCreateParameter parameter = (ShowcaseClusterCreateParameter)Parameter;

            Title = parameter.Title;

            Map(parameter.SelectedShowcaseCluster, parameter.CategoryId);
        }

        protected override Task HandleOkAsync()
        {
            CloseOk();
            return Task.CompletedTask;
        }

        private async Task RefreshCategoriesAsync()
        {
            PagedResult<CategoryDto> categories = await WebClient.ExecuteApiRequestAsync(new QueryCategories(), true);

            Categories = categories.Data
                .Where(x => x.Active > 0 && x.IsParent)
                .OrderBy(x => x.Name)
                .ToReadOnlyObservableCollection();
        }

        private async Task RefreshClustersAsync()
        {
            List<ClusterDto> clusters = await WebClient.ExecuteApiRequestAsync(new QueryClusters());

            Clusters = clusters.Select(x => new ComboBoxItem(x.Id, x.Name)).ToReadOnlyObservableCollection();
        }

        private void Map(ShowcaseClusterViewItem selectedItem, int? categoryId)
        {
            if (categoryId.HasValue)
            {
                SelectedCategory = Categories.FirstOrDefault(x => x.Id == categoryId);
            }

            if (selectedItem is not null)
            {
                Quantity = selectedItem.QuantityLocation;
                SelectedCluster = Clusters.FirstOrDefault(x => x.Id == selectedItem.ClusterId);
            }
        }
    }
}