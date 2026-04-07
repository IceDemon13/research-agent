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
using Telemart.Client.Data.Requests.Features.FeatureGroup;
using Telemart.Client.Data.Requests.Features.ParserFeature;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Data.WebClient.Security;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.Content;
using Telemart.Client.TransferObjects.Paging;
using Telemart.Client.ViewModels.Directories.Category;

namespace Telemart.Client.ViewModels.Parser.Dictionary
{
    public class FeatureParserDictionaryViewModel : ParserDictionaryViewModelBase
    {
        private readonly IErrorHandler errorHandler;

        private IReadOnlyCollection<FeatureGroupDto> featureGroups;

        public FeatureParserDictionaryViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            IMapper mapper,
            IErrorHandler errorHandler)
            : base(webClient, dictionaries, messageFacadeService, mapper)
        {
            this.errorHandler = errorHandler;
            IsTemplateVisible = WebClient.IsOperationAllowed(BusinessOperation.ParserSearchTemplateModuleAccess);
        }

        public FeatureParserDictionaryViewModel()
        {
        }

        protected override void CompareAliasAuto(ParserAliasViewItemWrapper alias)
        {
            ParserAliasDto parserAliasDto = aliasesDictionary[alias.Id];

            FeatureGroupDto[] featureGroupCandidates = featureGroups
                .Where(x => parserAliasDto.CategoryIds.Contains(x.CategoryId))
                .ToArray();

            IReadOnlyCollection<ProductComparsionDto> proposedValues = featureGroupCandidates
                .SelectMany(x => x.Features
                    .Where(y => y.Name == parserAliasDto.Name)
                    .Select(y => new ProductComparsionDto
                    {
                        Id = y.Id,
                        Name = $"{x.Name}/{y.Name}",
                        ParentCategoryId = x.CategoryId
                    })).ToArray();

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
            return WordEndingHelper.GetWordByNumber(number, new[] { "характеристика", "характеристики", "характеристик" });
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

                            sheet.Name = "Характеристики";

                            using (IXlRow row = sheet.CreateRow())
                            {
                                foreach (string header in headers)
                                {
                                    using (IXlCell cell = row.CreateCell())
                                    {
                                        cell.Value = header;
                                        cell.ApplyFormatting(headerRowFormatting);
                                    }
                                }
                            }

                            foreach (ParserAliasDto alias in aliasesToExport)
                            {
                                using (IXlRow row = sheet.CreateRow())
                                {
                                    foreach (string header in headers)
                                    {
                                        using (IXlCell cell = row.CreateCell())
                                        {
                                            cell.Value = GetValueByHeader(header, alias);
                                            cell.ApplyFormatting(cellFormatting);
                                        }
                                    }
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

            IEnumerable<FeatureGroupDto> featureGroupsToSelect = featureGroups.Where(x => source.CategoryIds.Contains(x.CategoryId));

            ParserAliasState imageState = (ParserAliasState)Enum.Parse(typeof(ParserAliasState), source.StateId.ToString());

            ComparsionItem selectedComparsionItem = ComparsionItem.NotAssosiated;

            foreach (FeatureGroupDto featureGroupDto in featureGroupsToSelect.OrderBy(x => x.Position))
            {
                foreach (FeatureDto featureDto in featureGroupDto.Features.OrderBy(x => x.Position))
                {
                    ComparsionItem comparsionItem;

                    if (featureDto.Id == item.ProductId)
                    {
                        int stateId = source.StateId == (int)ParserAliasState.AssociatedAuto
                            ? (int)ParserAliasState.Associated
                            : source.StateId;

                        comparsionItem = new ComparsionItem(stateId, featureDto.Id, $"{featureGroupDto.Name} => {featureDto.Name}", imageState);

                        selectedComparsionItem = comparsionItem;
                    }
                    else
                    {
                        comparsionItem = new ComparsionItem((int)ParserAliasState.Associated, featureDto.Id, $"{featureGroupDto.Name} => {featureDto.Name}", ParserAliasState.Associated);
                    }

                    comparsionItems.Add(comparsionItem);
                }
            }

            item.ComparsionItems = new ObservableCollection<ComparsionItem>(comparsionItems.OrderBy(x => x.StateId));
            item.SelectedComparsionItem = selectedComparsionItem;

            return item;
        }

        protected override async Task RefreshInternalAsync()
        {
            ConcurrentBag<FeatureGroupDto> concurrentCollection = new ConcurrentBag<FeatureGroupDto>();

            await Task.WhenAll((Filter?.SelectedCategories ?? Enumerable.Empty<CategoryViewItem>())
                .Select(x => FetchFeatureGroups(x.Id, concurrentCollection)));

            featureGroups = concurrentCollection.ToArray();

            async Task FetchFeatureGroups(int categoryId, ConcurrentBag<FeatureGroupDto> collection)
            {
                PagedResult<FeatureGroupDto> featureGroupsPage = await WebClient.ExecuteApiRequestAsync(new QueryFeatureGroups(categoryId, true));

                foreach (FeatureGroupDto featureGroup in featureGroupsPage.Data)
                {
                    collection.Add(featureGroup);
                }
            }
        }

        protected override async Task<IReadOnlyCollection<ParserAliasDto>> GetParserAliasesAsync(ParserAliasFilteringItem filteringItem)
        {
            List<ParserFeatureDto> parserFeatures = await WebClient.ExecuteApiRequestAsync(new QueryParserFeatures(filteringItem));

            return parserFeatures
                .Where(x => x.CategoryIds.Distinct().Count() == 1)
                .Select(Map)
                .ToArray();
        }

        protected override async Task<IReadOnlyCollection<ParserAliasDto>> UpdateParserAliasesAsync(IReadOnlyCollection<ParserAliasSaveDto> parserAliases)
        {
            IReadOnlyCollection<ParserFeatureSaveDto> parserFeaturesToSave = parserAliases
                .Select(x => new ParserFeatureSaveDto((int)x.Id, x.ProductId, x.StateId))
                .ToArray();

            IReadOnlyCollection<ParserAliasDto> updatedParserAliases = Array.Empty<ParserAliasDto>();

            (await errorHandler.HandleErrorsAsync(
                    _ => WebClient.ExecuteApiRequestAsync(new UpdateParserFeatures(parserFeaturesToSave)),
                    "обновлении словаря характеристик",
                    "Словарь характеристик обновлен",
                    this,
                    true))
                .IfNotNull(x => updatedParserAliases = x.Data.Select(Map).ToArray());

            return updatedParserAliases;
        }

        protected override Task DeleteParserAliasesAsync(long[] ids)
        {
            return errorHandler.HandleErrorsAsync(
                _ => WebClient.ExecuteApiRequestAsync(new DeleteParserFeatures(ids.Cast<int>().ToArray())),
                "удалении словаря характеристик",
                "Словарь характеристик удален",
                this,
                true);
        }

        private static ParserAliasDto Map(ParserFeatureDto parserFeature)
        {
            return new ParserAliasDto
            {
                Id = parserFeature.Id,
                Name = parserFeature.Name,
                CreatedOn = parserFeature.CreatedOn,
                ProductId = parserFeature.FeatureId,
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