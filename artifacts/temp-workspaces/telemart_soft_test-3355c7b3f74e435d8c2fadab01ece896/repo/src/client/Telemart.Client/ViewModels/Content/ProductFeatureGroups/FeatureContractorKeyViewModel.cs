using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using DevExpress.Mvvm.DataAnnotations;
using DevExpress.Mvvm.Native;
using Telemart.Client.Common.MvvmEnhancements;
using Telemart.Client.Common.Services;
using Telemart.Client.Data.Requests.Features.Contractor;
using Telemart.Client.Data.Requests.Features.ParserFeature;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Properties;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.Paging;
using Telemart.Client.ViewModels.Base;
using Telemart.Client.ViewModels.Parser.Dictionary;

namespace Telemart.Client.ViewModels.Content.ProductFeatureGroups
{
    public sealed class FeatureContractorKeyViewModel : TelemartDialogViewModelBase
    {
        public FeatureContractorKeyViewModel(
            IWebClient webClient,
            IMessageFacadeService messageFacadeService,
            IDictionaries dictionaries)
            : base(webClient, dictionaries, messageFacadeService)
        {
        }

        public ComboBoxItem? Contractor
        {
            get { return GetProperty(() => Contractor); }
            set { SetProperty(() => Contractor, value); }
        }

        public ReadOnlyObservableCollection<ComboBoxItem> Contractors
        {
            get { return GetProperty(() => Contractors); }
            set { SetProperty(() => Contractors, value); }
        }

        public string Key
        {
            get { return GetProperty(() => Key); }
            set { SetProperty(() => Key, value); }
        }

        public static void BuildMetadata(MetadataBuilder<FeatureContractorKeyViewModel> builder)
        {
            builder.Property(x => x.Contractor)
                .Required(() => Resources.RequiredErrorMessage);
            builder.Property(x => x.Key)
                .Required(() => Resources.RequiredErrorMessage);
        }

        protected override async Task HandleLoadedAsync()
        {
            FeatureContractorKeyViewItem parameter = (FeatureContractorKeyViewItem)Parameter;

            if (parameter != null)
            {
                Contractor = new ComboBoxItem(parameter.ContractorId, parameter.ContractorName);
                Key = parameter.Key;
            }

            PagedResult<ContractorDto> result = await WebClient.ExecuteApiRequestAsync(new QueryContractors(true));

            Contractors = result.Data
                .Where(x => x.IsCompetitor || x.IsSupplier)
                .Select(x => new ComboBoxItem(x.Id, x.Name))
                .ToReadOnlyObservableCollection();

            ParserAliasFilteringItem aliasFilter = new(Contractors.Select(x => x.Id).ToList());

            List<ParserFeatureDto> parserFeatures = await WebClient.ExecuteApiRequestAsync(new QueryParserFeatures(aliasFilter));

            Contractors = Contractors
                .OrderByDescending(x => parserFeatures.Any(z => z.ContractorId == x.Id))
                .ToReadOnlyObservableCollection();

            await base.HandleLoadedAsync();

            Title = "Настройка категории";
        }

        protected override Task HandleOkAsync()
        {
            CloseOk();

            return Task.CompletedTask;
        }
    }
}