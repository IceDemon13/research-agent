using System.Threading.Tasks;
using AutoMapper;
using Telemart.Client.Common.Services;
using Telemart.Client.Data.Requests.Features.Catalog;
using Telemart.Client.Data.Requests.Features.Catalog.TransferObjects;
using Telemart.Client.Data.Requests.Features.Setting;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.AssembledComputerRule
{
    public class SelectComplectationGeneratorOptionsViewModel : TelemartDialogViewModelBase
    {
        public SelectComplectationGeneratorOptionsViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            IMapper mapper)
            : base(webClient, dictionaries, messageFacadeService)
        {
            Mapper = mapper;
        }

        public ComplactationGeneratorOptionsItem Model
        {
            get { return GetProperty(() => Model); }
            set { SetProperty(() => Model, value); }
        }

        private IMapper Mapper { get; }

        protected override async Task HandleLoadedAsync()
        {
            ComplectationGeneratorOptionsDto complactationGeneratorOptionsDto = await WebClient.ExecuteCatalogApiRequestAsync(new QueryComplectationGeneratorOptions());

            string overPricePercentStr = await WebClient.ExecuteApiRequestAsync(new QueryAssemblyPriceDeviation());

            Model = Mapper.Map<ComplactationGeneratorOptionsItem>(complactationGeneratorOptionsDto);

            if (decimal.TryParse(overPricePercentStr, out decimal overPricePercent))
            {
                Model.OverPricePercent = overPricePercent;
            }

            Title = "Источники";
        }

        protected override Task HandleOkAsync()
        {
            CloseOk();

            return Task.CompletedTask;
        }
    }
}