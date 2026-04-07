using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Telemart.Client.Common.MvvmEnhancements;
using Telemart.Client.Data.Requests.Features.Contractor;
using Telemart.Client.Data.Requests.Features.ParserSettings;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Data.WebClient.Security;
using Telemart.Client.Extensions;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.ParserSettings;

namespace Telemart.Client.ViewModels.Parser.Dictionary
{
    public sealed class ProductParserDictionaryFilter : ParserDictionaryFilter
    {
        public ProductParserDictionaryFilter(IMapper mapper)
            : base(mapper)
        {
        }

        protected override async Task RefreshContractorsAsync(IWebClient webClient)
        {
            if (Contractors == null || Contractors.Count == 0)
            {
                Task<List<ContractorDto>> getContractorsTask = webClient.ExecuteApiRequestAsync(new QueryContractors(), true).GetPagedResultDataAsync();
                Task<List<ParserSettingsDto>> getParsersTask = webClient.ExecuteApiRequestAsync(new QueryParsersSettings(), true);

                await Task.WhenAll(getContractorsTask, getParsersTask);

                HashSet<int> contractorIds = getParsersTask.Result.Select(x => x.ContractorId).ToHashSet();

                bool showAll = webClient.IsOperationAllowed(BusinessOperation.ShowNerdpartIndictionary);

                IEnumerable<ComboBoxItem> contractorItems = getContractorsTask.Result
                    .Where(x => contractorIds.Contains(x.Id) && (showAll || !x.Name.Contains("NerdPart", StringComparison.OrdinalIgnoreCase)))
                    .OrderBy(x => x.Name)
                    .Select(x => new ComboBoxItem(x.Id, x.Name));

                Contractors = new ObservableCollection<ComboBoxItem>(contractorItems);
                contractorsDictionary = getContractorsTask.Result.ToDictionary(x => x.Id, x => x.Name);
            }
        }
    }
}