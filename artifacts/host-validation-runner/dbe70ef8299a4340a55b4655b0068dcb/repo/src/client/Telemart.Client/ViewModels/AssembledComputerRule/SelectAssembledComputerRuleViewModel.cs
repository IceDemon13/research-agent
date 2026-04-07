using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using DevExpress.Mvvm.Native;
using Telemart.Client.Common.Services;
using Telemart.Client.Data.Requests.Features.AssembledComputerRule;
using Telemart.Client.Data.Requests.Features.Catalog;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.TransferObjects;
using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.AssembledComputerRule
{
    public sealed class SelectAssembledComputerRuleViewModel : TelemartDialogViewModelBase
    {
        public SelectAssembledComputerRuleViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService)
            : base(webClient, dictionaries, messageFacadeService)
        {
            SelectedProducts = new ObservableCollection<SelectAssembledComputerRuleProductViewItem>();
        }

        public ObservableCollection<SelectAssembledComputerRuleProductViewItem> SelectedProducts
        {
            get { return GetProperty(() => SelectedProducts); }
            set { SetProperty(() => SelectedProducts, value); }
        }

        public ReadOnlyObservableCollection<SelectAssembledComputerRuleProductViewItem> Products
        {
            get { return GetProperty(() => Products); }
            private set { SetProperty(() => Products, value); }
        }

        protected override async Task HandleLoadedAsync()
        {
            SelectAssembledComputerRuleParameter parameter = (SelectAssembledComputerRuleParameter)Parameter;

            List<AssembledComputerRuleDto> assembledComputerRules = await WebClient.ExecuteApiRequestAsync(new QueryAssembledComputerRules());

            assembledComputerRules = assembledComputerRules.Where(x => x.Active).ToList();

            QueryProductByIdsDto queryProductByIdsDto = new(assembledComputerRules.Select(x => x.ProductId).ToArray(), parameter.ContractorId, queryAdditionalServices: true);

            List<ProductDto> products = await WebClient.ExecuteCatalogApiRequestAsync(new QueryProductByIds(queryProductByIdsDto));

            if (!products.Any())
            {
                MessageFacadeService.ShowNotificationWarning("Нет активных конфигураций");
                IsOk = false;
                Close();
            }

            Products = assembledComputerRules.Select(x => new SelectAssembledComputerRuleProductViewItem
                {
                    ProductId = x.ProductId,
                    ProductName = x.Name,
                    AssembledComputerRuleId = x.Id,
                    Quantity = 1,
                    ProductLink = x.Link,
                    Price = products.First(z => z.Id == x.ProductId).Price
                })
                .OrderBy(x => x.Price)
                .ToReadOnlyObservableCollection();

            await base.HandleLoadedAsync();

            Title = "Выбор конфигураций ПК";
        }

        protected override Task HandleOkAsync()
        {
            if (SelectedProducts?.Any() != true)
            {
                MessageFacadeService.ShowNotificationWarning("Конфигурация не выбрана");
                return Task.CompletedTask;
            }

            IsOk = true;
            Close();

            return Task.CompletedTask;
        }
    }
}
