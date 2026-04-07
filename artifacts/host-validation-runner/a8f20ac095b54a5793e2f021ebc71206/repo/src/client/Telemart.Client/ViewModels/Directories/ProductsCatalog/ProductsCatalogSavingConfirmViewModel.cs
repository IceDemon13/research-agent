using System.Collections.Generic;
using System.Threading.Tasks;
using Telemart.Client.Common.Services;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.Directories.ProductsCatalog
{
    public sealed class ProductsCatalogSavingConfirmViewModel : TelemartDialogViewModelBase
    {
        public ProductsCatalogSavingConfirmViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService)
            : base(webClient, dictionaries, messageFacadeService)
        {
        }

        public ICollection<ProductCatalogViewItemWrapper> ItemsToSave
        {
            get { return GetProperty(() => ItemsToSave); }
            set { SetProperty(() => ItemsToSave, value); }
        }

        protected override Task HandleLoadedAsync()
        {
            ItemsToSave = (List<ProductCatalogViewItemWrapper>)Parameter;
            Title = "Сохранение товаров";

            return Task.CompletedTask;
        }

        protected override Task HandleOkAsync()
        {
            IsOk = true;
            Close();
            MessageFacadeService.ShowNotificationInfo("Saving not implemented");

            return Task.CompletedTask;
        }
    }
}
