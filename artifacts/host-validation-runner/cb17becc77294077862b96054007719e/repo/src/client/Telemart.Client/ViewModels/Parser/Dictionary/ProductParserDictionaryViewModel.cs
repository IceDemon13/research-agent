using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using DevExpress.Export.Xl;
using DevExpress.Mvvm;
using Microsoft.Extensions.Options;
using Telemart.Client.Business.Parser;
using Telemart.Client.Common;
using Telemart.Client.Common.ErrorHandler;
using Telemart.Client.Common.Services;
using Telemart.Client.Core.Helpers;
using Telemart.Client.Data.Options;
using Telemart.Client.Data.Requests.Features.Catalog;
using Telemart.Client.Data.Requests.Features.Parser;
using Telemart.Client.Data.Requests.Features.Parser.Actions;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.ViewModels.Parser.Dictionary
{
    public sealed class ProductParserDictionaryViewModel : ParserDictionaryViewModelBase
    {
        private Dictionary<int, ProductComparsionDto> productsDictionary;

        public ProductParserDictionaryViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            IMapper mapper,
            IComparsionManager comparisonManager,
            IOptionsSnapshot<CatalogServiceOptions> catalogOptions,
            IErrorHandler errorHandler)
            : base(webClient, dictionaries, messageFacadeService, mapper)
        {
            ComparisonManager = comparisonManager ?? throw new ArgumentNullException(nameof(comparisonManager));
            ErrorHandler = errorHandler ?? throw new ArgumentNullException(nameof(errorHandler));
            AutoCompareByFeatures = true;

            CompareAutoByFeaturesCommand = new DelegateCommand(CompareAutoByFeatures);

            Filter = new ProductParserDictionaryFilter(mapper);

            CatalogOptions = catalogOptions.Value;
        }

        public IDelegateCommand CompareAutoByFeaturesCommand { get; }

        private IErrorHandler ErrorHandler { get; }

        private IComparsionManager ComparisonManager { get; }

        private CatalogServiceOptions CatalogOptions { get; }

        public ProductParserDictionaryViewModel()
        {
        }

        protected override async Task<IReadOnlyCollection<ParserAliasDto>> UpdateParserAliasesAsync(IReadOnlyCollection<ParserAliasSaveDto> parserAliases)
        {
            return await WebClient.ExecuteApiRequestAsync(new UpdateProductsParserAliases(parserAliases));
        }

        protected override void CompareAliasAuto(ParserAliasViewItemWrapper alias)
        {
            ParserAliasDto parserAliasDto = aliasesDictionary[alias.Id];

            ComparsionResult comparisonResult = ComparisonManager.Compare(parserAliasDto, productsDictionary.Values);

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

        protected override async Task CompareAutoAsync()
        {
            await base.CompareAutoAsync();

            await SearchProductsInCatalogAsync();
        }

        protected override Task DeleteParserAliasesAsync(long[] ids)
        {
            return WebClient.ExecuteApiRequestAsync(new DeleteProductsParserAliases(ids));
        }

        protected override string DeclOfNum(int number)
        {
            return WordEndingHelper.GetWordByNumber(number, new[] { "товар", "товара", "товаров" });
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
                            string[] headers = { "id_product", "id_category", "manufactor", "name", "model", "color", "pn", "keywords", "ym_id", "active" };

                            sheet.Name = "Товары";

                            foreach (string header in headers)
                            {
                                using (IXlColumn column = sheet.CreateColumn())
                                {
                                    switch (header)
                                    {
                                        case "name":
                                        case "model":
                                            column.WidthInCharacters = 60;
                                            break;
                                        case "pn":
                                            column.WidthInCharacters = 20;
                                            break;
                                    }
                                }
                            }

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
        }

        protected override async Task RefreshInternalAsync()
        {
            List<ProductComparsionDto> parserProducts = await WebClient.ExecuteApiRequestAsync(new QueryParserProducts());

            productsDictionary = parserProducts.ToDictionary(x => x.Id);
        }

        protected override async Task<IReadOnlyCollection<ParserAliasDto>> GetParserAliasesAsync(ParserAliasFilteringItem filteringItem)
        {
            return await WebClient.ExecuteApiRequestAsync(new QueryProductsParserAliases(filteringItem));
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

            ComparsionItem selectedComparsionItem;

            if (item.ProductId.HasValue)
            {
                int stateId = source.StateId == (int)ParserAliasState.AssociatedAuto
                    ? (int)ParserAliasState.Associated
                    : source.StateId;

                int productId = item.ProductId.Value;

                ParserAliasState imageState = (ParserAliasState)Enum.Parse(typeof(ParserAliasState), source.StateId.ToString());

                string name = productsDictionary.TryGetValue(productId, out ProductComparsionDto product)
                    ? product.Name
                    : $"Товар №{productId.ToString(CultureInfo.InvariantCulture)}";

                selectedComparsionItem = new ComparsionItem(stateId, productId, name, imageState);

                comparsionItems.Add(selectedComparsionItem);
            }
            else
            {
                selectedComparsionItem = comparsionItems.FirstOrDefault(x => x.StateId == item.StateId);
            }

            item.ComparsionItems = new ObservableCollection<ComparsionItem>(comparsionItems.OrderBy(x => x.StateId));
            item.SelectedComparsionItem = selectedComparsionItem;

            return item;
        }

        private static string GetValueByHeader(string header, ParserAliasDto parserAlias)
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
                case "model":
                    value = parserAlias.Name;
                    break;
                case "pn":
                    value = parserAlias.PartNumber ?? string.Empty;
                    break;
                case "active":
                    value = "0.5";
                    break;
            }

            return value;
        }

        private void CompareAutoByFeatures()
        {
            SizeableDialogDocumentManagerService.ShowView<CompareProductByFeaturesViewModel>(null, this);
        }

        private async Task SearchProductsInCatalogAsync()
        {
            IReadOnlyCollection<ParserAliasViewItemWrapper> toProcess = GetSelectedItemsToProcess()
                .Where(x => x.ComparsionResult == null || x.ComparsionResult?.ProposedValuesCount == 0)
                .ToArray();

            ProgressScreenViewModel progressViewModel = new ProgressScreenViewModel("Поиск товаров", toProcess.Count);

            using CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();

            Task<int> compareTask = SearchProductsInCatalogAsync(cancellationTokenSource.Token);

            DialogDocumentManagerService.ShowView("ProgressScreenView", progressViewModel);

            if (!progressViewModel.IsOk)
            {
                cancellationTokenSource.Cancel();
            }

            try
            {
                int items = await compareTask;
                MessageFacadeService.ShowNotificationInfo($"Сопоставлено {items} {DeclOfNum(items)}");
            }
            catch (OperationCanceledException)
            {
                MessageFacadeService.ShowNotificationWarning($"Отмена. Сопоставлено {progressViewModel.ProcessedCount} {DeclOfNum(progressViewModel.ProcessedCount)}");
            }

            async Task<int> SearchProductsInCatalogAsync(CancellationToken cancellationToken)
            {
                int processed = 0;

                foreach (IReadOnlyCollection<ParserAliasViewItemWrapper> productSection in toProcess.Section(10))
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        return processed;
                    }

                    await ErrorHandler.HandleErrorsAsync(x => SearchProductsInCatalogInternalAsync(productSection), "поиске товаров", "Товары найдены", this, false, false, cancellationToken: cancellationToken);

                    processed += productSection.Count;

                    progressViewModel.SetProcessedCount(processed);
                }

                return toProcess.Count;
            }

            async Task<int?> SearchProductsInCatalogInternalAsync(IReadOnlyCollection<ParserAliasViewItemWrapper> productSection)
            {
                string[] patterns = productSection.Select(x => x.Name).ToArray();

                QueryProductByNames gatewayRequest = new QueryProductByNames(
                    Constants.TelemartContractorId,
                    patterns,
                    Language.RussianId,
                    false,
                    categoryIds: Filter.SelectedCategories?.Select(x => x.Id).ToArray(),
                    tags: new[] { "parser_aliases" },
                    take: 5,
                    precision: CatalogOptions.ElasticPrecisionTuning);

                List<ProductSearchResponseDto> foundedProducts = await WebClient.ExecuteCatalogApiRequestAsync(gatewayRequest);

                foreach (ParserAliasViewItemWrapper product in productSection)
                {
                    ProductSearchResponseDto foundedProduct = foundedProducts.FirstOrDefault(x => x.Pattern == product.Name);

                    if (foundedProduct?.Products?.Any() == true)
                    {
                        product.ComparsionResult = new ComparsionResult(foundedProduct.Products
                            .OrderByDescending(x => x.Ratio)
                            .Select(x => new ProductComparsionDto
                            {
                                Id = x.Product.Id,
                                Manufactor = x.Product.Manufactor,
                                Name = x.Product.Name,
                                ParentCategoryId = x.Product.ParentCategoryId,
                                PartNumber = x.Product.Pn
                            }).ToArray());
                    }
                }

                return productSection.Count;
            }
        }
    }
}