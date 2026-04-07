using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Telemart.Client.Common.Services;
using Telemart.Client.Data.Requests.Features.Movement.Actions;
using Telemart.Client.Data.Requests.Features.Warehouse;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.Helpers;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.Warehouse;
using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.Warehouse.Movement
{
    public class ShowcaseRoutesViewModel : TelemartDialogViewModelBase
    {
        public ShowcaseRoutesViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            ILogger<ShowcaseRoutesViewModel> logger)
        : base(webClient, dictionaries, messageFacadeService, logger)
        {
        }

        public IReadOnlyCollection<MovementShowcaseDataDto> Products
        {
            get { return GetProperty(() => Products); }
            set { SetProperty(() => Products, value); }
        }

        public IReadOnlyCollection<WarehouseDto> Warehouses
        {
            get { return GetProperty(() => Warehouses); }
            set { SetProperty(() => Warehouses, value); }
        }

        protected override async Task HandleLoadedAsync()
        {
            (Products, Warehouses) = await TaskExt.WhenAll(
                WebClient.ExecuteApiRequestAsync(new GetShowcaseRoutes()),
                WebClient.ExecuteApiRequestAsync(new QueryWarehouses()).GetPagedResultDataAsync());

            Title = "Маршруты витрин";
        }

        protected override Task HandleOkAsync()
        {
            CloseOk();
            return Task.CompletedTask;
        }
    }
}