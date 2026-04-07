using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using DevExpress.Export.Xl;
using Telemart.Client.Business.Parser;
using Telemart.Client.Common.ErrorHandler;
using Telemart.Client.Common.Services;
using Telemart.Client.Core.Helpers;
using Telemart.Client.Data.Requests.Features.Content;
using Telemart.Client.Data.Requests.Features.ParserFeature;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.Content;

namespace Telemart.Client.ViewModels.Parser.Dictionary
{
    public class FeatureValueParserDictionaryViewModel : ParserDictionaryViewModelBase
    {
        private readonly IErrorHandler errorHandler;

        private IReadOnlyCollection<FeatureValueDto> featureValues;
        private IReadOnlyDictionary<int, ParserFeatureValueDto> parserFeatureValues;

        public FeatureValueParserDictionaryViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            IMapper mapper,
            IErrorHandler errorHandler)
            : base(webClient, dictionaries, messageFacadeService, mapper)
        {
            this.errorHandler = errorHandler;
            FeatureColumnVisible = true;
            CategoryColumnVisible = false;
        }

        public FeatureValueParserDictionaryViewModel()
        {
        }

        protected override void CompareAliasAuto(ParserAliasViewItemWrapper alias)
        {
            ParserFeatureValueDto parserFeatureValueDto = parserFeatureValues[(int)alias.Id];

            FeatureValueDto[] featureValueCandidates = featureValues
                .Where(x => parserFeatureValueDto.FeatureId == x.FeatureId)
                .ToArray();

            IReadOnlyCollection<ProductComparsionDto> proposedValues = featureValueCandidates
                    .Where(y => y.Value == parserFeatureValueDto.Name || y.ValueUkr == parserFeatureValueDto.Name || y.ValueEn == parserFeatureValueDto.Name)
                    .Select(y => new ProductComparsionDto
                    {
                        Id = y.Id,
                        Name = y.Value
                    }).ToArray();

            ProductComparsionDto guaranteedValue = proposedValues.Count == 1
                ? proposedValues.First()
                : null;

            ComparsionResult comparisonResult = new ComparsionResult(proposedValues, guaranteedValue);

            alias.ComparsionResult = comparisonResult;

            if (alias.NewStateId == (int)ParserAliasState.NotAssosiated && comparisonResult.GuaranteedValue != null)
            {
                alias.SelectedComparsionItem = new ComparsionItem(
                    (int)ParserAliasState.Associated,
                    comparisonResult.GuaranteedValue.Id,
                    comparisonResult.GuaranteedValue.Name,
                    ParserAliasState.Associated);
            }
        }

        protected override string DeclOfNum(int number)
        {
            return WordEndingHelper.GetWordByNumber(number, new[] { "значение характеристики", "значения характеристики", "значений характеристик" });
        }

        protected override bool ExportToExcel(string filePath, IReadOnlyCollection<ParserAliasDto> aliasesToExport)
        {
            bool exported = false;

            IXlExporter exporter = XlExport.CreateExporter(XlDocumentFormat.Xlsx);

            try
            {
                using (FileStream stream = new FileStream(filePath, FileMode.Create))
                {
                    using (IXlDocument document = exporter.CreateDocument(stream))
                    {
                        XlCellFormatting cellFormatting = new XlCellFormatting
                        {
                            Font = new XlFont { Name = "Times New Roman", SchemeStyle = XlFontSchemeStyles.None }
                        };

                        XlCellFormatting headerRowFormatting = new XlCellFormatting();
                        headerRowFormatting.CopyFrom(cellFormatting);
                        headerRowFormatting.Font.Bold = true;

                        using (IXlSheet sheet = document.CreateSheet())
                        {
                            string[] headers = { "id_feature", "id_category", "name" };

                            sheet.Name = "Значения характеристик";

                            using (IXlRow row = sheet.CreateRow())
                            {
                                foreach (string header in headers)
                                {
                                    using IXlCell cell = row.CreateCell();

                                    cell.Value = header;
                                    cell.ApplyFormatting(headerRowFormatting);
                                }
                            }

                            foreach (ParserAliasDto alias in aliasesToExport)
                            {
                                using IXlRow row = sheet.CreateRow();

                                foreach (string header in headers)
                                {
                                    using IXlCell cell = row.CreateCell();

                                    cell.Value = GetValueByHeader(header, alias);
                                    cell.ApplyFormatting(cellFormatting);
                                }
                            }
                        }
                    }
                }

                exported = true;
            }
            catch (IOException exception)
            {
                MessageFacadeService.ShowNotificationWarning(exception.Message);
            }

            return exported;

            static string GetValueByHeader(string header, ParserAliasDto parserAlias)
            {
                string value = string.Empty;

                switch (header)
                {
                    case "id_category":
                        value = parserAlias.CategoryIds.Count == 1
                            ? parserAlias.CategoryIds.First().ToString()
                            : string.Empty;
                        break;
                    case "name":
                        value = parserAlias.Name;
                        break;
                }

                return value;
            }
        }

        protected override ParserAliasViewItem MapTransferObjectToViewItem(ParserAliasDto source)
        {
            ParserAliasViewItem item = new ParserAliasViewItem
            {
                Id = source.Id,
                StateId = source.StateId,
                ProductId = source.ProductId,
                Name = source.Name,
                FeatureName = source.FeatureName,
                PartNumber = source.PartNumber,
                CreatedOn = source.CreatedOn,
                ModifiedOn = source.ModifiedOn,
                ModifiedById = source.ModifiedById,
                PostponedTo = source.PostponedTo,
                ComparsionResult = null
            };

            List<ComparsionItem> comparsionItems = new List<ComparsionItem>
            {
                ComparsionItem.NotAssosiated,
                ComparsionItem.Postponed,
                ComparsionItem.Ignored
            };

            ParserFeatureValueDto parserFeatureValueDto = parserFeatureValues[(int)source.Id];

            IEnumerable<FeatureValueDto> featureValuesToSelect = featureValues.Where(x => parserFeatureValueDto.FeatureId == x.FeatureId);

            ParserAliasState imageState = (ParserAliasState)Enum.Parse(typeof(ParserAliasState), source.StateId.ToString());

            ComparsionItem selectedComparsionItem = ComparsionItem.NotAssosiated;

            foreach (FeatureValueDto featureValue in featureValuesToSelect.OrderBy(x => x.Value))
            {
                ComparsionItem comparsionItem;

                if (featureValue.Id == item.ProductId)
                {
                    int stateId = source.StateId == (int)ParserAliasState.AssociatedAuto
                        ? (int)ParserAliasState.Associated
                        : source.StateId;

                    comparsionItem = new ComparsionItem(stateId, featureValue.Id, featureValue.Value, imageState);

                    selectedComparsionItem = comparsionItem;
                }
                else
                {
                    comparsionItem = new ComparsionItem((int)ParserAliasState.Associated, featureValue.Id, featureValue.Value, ParserAliasState.Associated);
                }

                comparsionItems.Add(comparsionItem);
            }

            item.ComparsionItems = new ObservableCollection<ComparsionItem>(comparsionItems.OrderBy(x => x.StateId));
            item.SelectedComparsionItem = selectedComparsionItem;

            return item;
        }

        protected override Task RefreshInternalAsync()
        {
            return Task.CompletedTask;
        }

        protected override async Task<IReadOnlyCollection<ParserAliasDto>> GetParserAliasesAsync(ParserAliasFilteringItem filteringItem)
        {
            List<ParserFeatureValueDto> parserFeatures = await WebClient.ExecuteApiRequestAsync(new QueryParserFeatureValues(filteringItem));

            parserFeatureValues = parserFeatures.ToDictionary(x => x.Id);

            int[] featureIds = parserFeatures.Select(x => x.FeatureId).Distinct().ToArray();

            ConcurrentBag<FeatureValueExDto> concurrentCollection = new();

            await Task.WhenAll(featureIds.Select(x => FetchFeatureValues(x, concurrentCollection)));

            featureValues = concurrentCollection.ToArray();

            return parserFeatures.Select(Map).ToArray();

            async Task FetchFeatureValues(int featureId, ConcurrentBag<FeatureValueExDto> collection)
            {
                List<FeatureValueExDto> currentFeatureValues = await WebClient.ExecuteApiRequestAsync(new QueryFeatureValues(featureId));

                foreach (FeatureValueExDto featureValue in currentFeatureValues)
                {
                    collection.Add(featureValue);
                }
            }
        }

        protected override async Task<IReadOnlyCollection<ParserAliasDto>> UpdateParserAliasesAsync(IReadOnlyCollection<ParserAliasSaveDto> parserAliases)
        {
            IReadOnlyCollection<ParserFeatureValueSaveDto> parserFeatureValuesToSave = parserAliases
                .Select(x => new ParserFeatureValueSaveDto((int)x.Id, x.ProductId, x.StateId))
                .ToArray();

            IReadOnlyCollection<ParserAliasDto> updatedParserAliases = Array.Empty<ParserAliasDto>();

            (await errorHandler.HandleErrorsAsync(
                    _ => WebClient.ExecuteApiRequestAsync(new UpdateParserFeatureValues(parserFeatureValuesToSave)),
                    "обновлении словаря значений характеристик",
                    "Словарь значений характеристик обновлен",
                    this,
                    true))
                .IfNotNull(x => updatedParserAliases = x.Data.Select(Map).ToArray());

            return updatedParserAliases;
        }

        protected override Task DeleteParserAliasesAsync(long[] ids)
        {
            return errorHandler.HandleErrorsAsync(
                _ => WebClient.ExecuteApiRequestAsync(new DeleteParserFeatureValues(ids.Cast<int>().ToArray())),
                "удалении словаря значений характеристик",
                "Словарь значений характеристик удален",
                this,
                true);
        }

        private static ParserAliasDto Map(ParserFeatureValueDto parserFeature)
        {
            return new ParserAliasDto
            {
                Id = parserFeature.Id,
                Name = parserFeature.Name,
                FeatureName = parserFeature.FeatureName,
                CreatedOn = parserFeature.CreatedOn,
                ProductId = parserFeature.FeatureValueId,
                StateId = parserFeature.StateId,
                ContractorProducts = new[]
                {
                    new ParserContractorProductDto
                    {
                        ContractorId = parserFeature.ContractorId
                    }
                },
                CategoryIds = parserFeature.CategoryIds.Distinct().ToArray()
            };
        }
    }
}