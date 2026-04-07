using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using Telemart.Client.Business.Parser;
using Telemart.Client.Common.MvvmEnhancements;
using Telemart.Client.Data.Requests.Features.Category;
using Telemart.Client.Data.Requests.Features.Contractor;
using Telemart.Client.Data.Requests.Features.Parser;
using Telemart.Client.Data.Requests.Features.ParserSettings;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Extensions;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.ParserSettings;
using Telemart.Client.ViewModels.Directories.Category;

namespace Telemart.Client.ViewModels.Parser.Dictionary
{
    public class ParserDictionaryFilter : BindableBase
    {
        private Dictionary<int, string> categoriesDictionary;
        protected Dictionary<int, string> contractorsDictionary;

        public ParserDictionaryFilter(IMapper mapper)
        {
            Mapper = mapper;
            SelectedCategories = new ObservableCollection<CategoryViewItem>();
        }

        public ObservableCollection<CategoryViewItem> Categories
        {
            get { return GetProperty(() => Categories); }
            private set { SetProperty(() => Categories, value); }
        }

        public Dictionary<int, string> CategoriesDictionary => categoriesDictionary;

        public Dictionary<int, string> ContractorsDictionary => contractorsDictionary;

        public ObservableCollection<ComboBoxItem> Contractors
        {
            get { return GetProperty(() => Contractors); }
            protected set { SetProperty(() => Contractors, value); }
        }

        public string Name
        {
            get { return GetProperty(() => Name); }
            set { SetProperty(() => Name, value); }
        }

        public List<ParserAliasStateDto> ParserAliasStates
        {
            get { return GetProperty(() => ParserAliasStates); }
            private set { SetProperty(() => ParserAliasStates, value); }
        }

        public string ProductId
        {
            get { return GetProperty(() => ProductId); }
            set { SetProperty(() => ProductId, value); }
        }

        public int RecordsCount
        {
            get { return GetProperty(() => RecordsCount); }
            set { SetProperty(() => RecordsCount, value); }
        }

        public ObservableCollection<CategoryViewItem> SelectedCategories
        {
            get { return GetProperty(() => SelectedCategories); }
            set { SetProperty(() => SelectedCategories, value); }
        }

        public List<object> SelectedContractors
        {
            get { return GetProperty(() => SelectedContractors); }
            set { SetProperty(() => SelectedContractors, value); }
        }

        public ObservableCollection<ParserAliasStateDto> SelectedStatuses
        {
            get { return GetProperty(() => SelectedStatuses); }
            set { SetProperty(() => SelectedStatuses, value); }
        }

        private IMapper Mapper { get; }

        public ParserAliasFilteringItem GetFilteringItem()
        {
            int? productId = int.TryParse(ProductId, out int productIdValue)
                ? (int?)productIdValue
                : null;

            return new ParserAliasFilteringItem(
                Name,
                productId,
                SelectedStatuses,
                SelectedCategories.Select(x => x.Id).ToList(),
                SelectedContractors?.Cast<ComboBoxItem>().Select(x => x.Id).ToList());
        }

        public Task RefreshValuesAsync(IWebClient webClient)
        {
            return Task.WhenAll(
                RefreshParserAliasStatesAsync(webClient),
                RefreshCategoriesAsync(webClient),
                RefreshContractorsAsync(webClient));
        }

        public void ResetFilterValues()
        {
            Name = null;
            ProductId = null;
            SelectedStatuses = new ObservableCollection<ParserAliasStateDto>(ParserAliasStates.Where(x => x.Id == (int)ParserAliasState.NotAssosiated || x.Id == (int)ParserAliasState.AssociatedAuto));
            SelectedCategories.Clear();
        }

        private async Task RefreshCategoriesAsync(IWebClient webClient)
        {
            if (Categories == null || Categories.Count == 0)
            {
                List<CategoryDto> categories = await webClient.ExecuteApiRequestAsync(new QueryCategories(), true).GetPagedResultDataAsync();

                Categories = categories
                    .Where(x => x.ParentLevel <= 0 && webClient.AuthenticatedEmployee.AllowCategories.Contains(x.Id))
                    .Select(x => Mapper.Map<CategoryViewItem>(x))
                    .ToObservableCollection();

                categoriesDictionary = categories.ToDictionary(x => x.Id, y => y.Name);
            }
        }

        protected virtual async Task RefreshContractorsAsync(IWebClient webClient)
        {
            if (Contractors == null || Contractors.Count == 0)
            {
                Task<List<ContractorDto>> getContractorsTask = webClient.ExecuteApiRequestAsync(new QueryContractors(), true).GetPagedResultDataAsync();
                Task<List<ParserSettingsDto>> getParsersTask = webClient.ExecuteApiRequestAsync(new QueryParsersSettings(), true);

                await Task.WhenAll(getContractorsTask, getParsersTask);

                HashSet<int> contractorIds = getParsersTask.Result.Select(x => x.ContractorId).ToHashSet();

                IEnumerable<ComboBoxItem> contractorItems = getContractorsTask.Result
                    .Where(x => contractorIds.Contains(x.Id))
                    .OrderBy(x => x.Name)
                    .Select(x => new ComboBoxItem(x.Id, x.Name));

                Contractors = new ObservableCollection<ComboBoxItem>(contractorItems);
                contractorsDictionary = getContractorsTask.Result.ToDictionary(x => x.Id, x => x.Name);
            }
        }

        private async Task RefreshParserAliasStatesAsync(IWebClient webClient)
        {
            if (ParserAliasStates == null || ParserAliasStates.Count == 0)
            {
                ParserAliasStates = await webClient.ExecuteApiRequestAsync(new QueryParserAliasStates(), true);
                SelectedStatuses = new ObservableCollection<ParserAliasStateDto>(ParserAliasStates.Where(x => x.Id == 1 || x.Id == 2));
            }
        }
    }
}