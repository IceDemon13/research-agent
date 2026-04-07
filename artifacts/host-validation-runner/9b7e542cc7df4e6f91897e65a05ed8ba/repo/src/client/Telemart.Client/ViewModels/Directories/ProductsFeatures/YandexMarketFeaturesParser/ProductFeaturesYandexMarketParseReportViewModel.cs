using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Telemart.Client.Common.ProductDescription;
using Telemart.Client.Common.Services;
using Telemart.Client.Core.Exceptions;
using Telemart.Client.Data.Requests.Features.Parser.Actions;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.Properties;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.Content;
using Telemart.Client.ViewModels.Base;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.ViewModels.Directories.ProductsFeatures.YandexMarketFeaturesParser
{
    public class ProductFeaturesYandexMarketParseReportViewModel : TelemartDialogViewModelBase
    {
        public ProductFeaturesYandexMarketParseReportViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            IProductDescriptionBuilder productDescriptionBuilder)
            : base(webClient, dictionaries, messageFacadeService)
        {
            ProductDescriptionBuilder = productDescriptionBuilder;
        }

        public ObservableCollection<ParserReportMessage> Messages
        {
            get { return GetProperty(() => Messages); }
            private set { SetProperty(() => Messages, value); }
        }

        public bool ParseIsComplete
        {
            get { return GetProperty(() => ParseIsComplete); }
            private set { SetProperty(() => ParseIsComplete, value); }
        }

        public int CategoriesCount
        {
            get { return GetProperty(() => CategoriesCount); }
            private set { SetProperty(() => CategoriesCount, value, () => RaisePropertyChanged(nameof(CategoriesText))); }
        }

        public int CurrentCategoryIndex
        {
            get { return GetProperty(() => CurrentCategoryIndex); }
            private set { SetProperty(() => CurrentCategoryIndex, value, () => RaisePropertyChanged(nameof(CategoriesText))); }
        }

        public int ProductsCount
        {
            get { return GetProperty(() => ProductsCount); }
            private set { SetProperty(() => ProductsCount, value, () => RaisePropertyChanged(nameof(ProductsText))); }
        }

        public int CurrentProductIndex
        {
            get { return GetProperty(() => CurrentProductIndex); }
            private set { SetProperty(() => CurrentProductIndex, value, () => RaisePropertyChanged(nameof(ProductsText))); }
        }

        public string CategoriesText => $"Категории: {CurrentCategoryIndex}/{CategoriesCount}";

        public string ProductsText => $"Товары: {CurrentProductIndex}/{ProductsCount}";

        public List<Dictionary<string, object>> ParsedProductFeatures { get; private set; }

        private IProductDescriptionBuilder ProductDescriptionBuilder { get; }

        private CancellationTokenSource TokenSource { get; } = new CancellationTokenSource();

        protected override Task HandleLoadedAsync()
        {
            Messages = new ObservableCollection<ParserReportMessage>();
            ParsedProductFeatures = new List<Dictionary<string, object>>();

            Title = "Парсинг";

            ParseIsComplete = false;

            Task.Factory.StartNew(
                p => ParseCategoriesAsync((ProductFeaturesYandexMarketParseParameter)p),
                Parameter,
                TokenSource.Token,
                TaskCreationOptions.LongRunning,
                TaskScheduler.FromCurrentSynchronizationContext());

            RaisePropertyChanged(nameof(ParseIsComplete));

            return Task.CompletedTask;
        }

        protected override Task HandleOkAsync()
        {
            IsOk = true;
            Close();
            return Task.CompletedTask;
        }

        protected override void HandleCancel()
        {
            TokenSource.Cancel();

            base.HandleCancel();
        }

        private async Task ParseCategoriesAsync(ProductFeaturesYandexMarketParseParameter parameter)
        {
            TokenSource.Token.ThrowIfCancellationRequested();

            var groupedProducts = parameter.Products
                .GroupBy(x => x.YandexCategoryHid)
                .Select(x => new
                {
                    x.Key,
                    Products = x.GroupBy(y => y.YandexId).ToDictionary(y => y.Key, y => y.Select(z => z.Name))
                })
                .ToList();

            CategoriesCount = groupedProducts.Count;
            CurrentCategoryIndex = 0;

            Regex regex = new Regex(@"#V\(""(.+?)""\)");

            HashSet<string> usedFeatures = new HashSet<string>(
                parameter.FeatureMap.SelectMany(x => GetAllMatches(regex.Matches(x.Value))),
                StringComparer.OrdinalIgnoreCase);

            try
            {
                foreach (var yandexCategory in groupedProducts)
                {
                    TokenSource.Token.ThrowIfCancellationRequested();

                    Messages.Add(new ParserReportMessage($"Парсинг категории hid={yandexCategory.Key}"));

                    IReadOnlyCollection<YandexMarketProductFeatureDto> features =
                        await ParseCategoryAsync(yandexCategory.Key, yandexCategory.Products.Keys);

                    TokenSource.Token.ThrowIfCancellationRequested();

                    if (features.Any())
                    {
                        HashSet<string> parsedFeatures = new HashSet<string>(features.Select(x => x.FeatureName), StringComparer.OrdinalIgnoreCase);

                        Messages.AddRange(GetFeatureUsageMessages(usedFeatures, parsedFeatures));

                        ParsedProductFeatures.AddRange(GetMappedFeatures(features, parameter.FeatureMap, yandexCategory.Products));

                        Messages.Add(new ParserReportMessage($"Парсинг категории hid={yandexCategory.Key} успешно завершен"));
                    }

                    CurrentCategoryIndex++;
                }

                ParseIsComplete = true;
            }
            catch (UnexpectedSatusException exception)
            {
                Messages.AddRange(exception.GetErrorItems().Select(x => new ParserReportMessage(x.Message)));
            }
            catch (UnexpectedErrorException exception)
            {
                Logger.LogError(exception, "Failed to parse product features");
                Messages.Add(new ParserReportMessage(Resources.ServerUnavailable));
            }
            catch (Exception exception)
            {
                Messages.Add(new ParserReportMessage("Ошибка при парсинге характеристик"));
                Logger.LogError(exception, "Error while parsing product features");
            }

            CurrentProductIndex = ProductsCount;
            CurrentCategoryIndex = CategoriesCount;
        }

        private async Task<IReadOnlyCollection<YandexMarketProductFeatureDto>> ParseCategoryAsync(int categoryHid, IReadOnlyCollection<string> productIds)
        {
            const int MaxParsedProductsCount = 100;

            List<YandexMarketProductFeatureDto> parsedFeatures = new List<YandexMarketProductFeatureDto>();

            ProductsCount = productIds.Count;
            CurrentProductIndex = 0;

            foreach (IReadOnlyCollection<string> partProductIds in productIds.Section(MaxParsedProductsCount))
            {
                Result<List<YandexMarketProductFeatureDto>> result = await WebClient.ExecuteApiRequestAsync(
                    new ParseYandexMarketProductFeatures(new YandexMarketCategoryProductsDto(categoryHid, partProductIds.ToArray())));

                parsedFeatures.AddRange(result.Data);

                CurrentProductIndex += partProductIds.Count;
            }

            if (!parsedFeatures.Any())
            {
                Messages.Add(new ParserReportMessage($"Не верный hid категории либо id товаров hid={categoryHid}, ids={string.Join(", ", productIds)}"));
            }

            return parsedFeatures;
        }

        private static IEnumerable<ParserReportMessage> GetFeatureUsageMessages(HashSet<string> usedFeatures, HashSet<string> parsedFeatures)
        {
            string[] usedNotParsedFeatures = (from u in usedFeatures
                                         join p in parsedFeatures on u.ToLower() equals p.ToLower() into used
                                         from us in used.DefaultIfEmpty()
                                         where us == null
                                         select u).ToArray();

            if (usedNotParsedFeatures.Any())
            {
                yield return new ParserReportMessage($"Заголовки не найденнные на сайте: {string.Join(", ", usedNotParsedFeatures)}", ParserReportMessage.RedColor);
            }

            string[] parsedNotUsedFeatures = (from p in parsedFeatures
                                              join u in usedFeatures on p.ToLower() equals u.ToLower() into parsed
                                              from ps in parsed.DefaultIfEmpty()
                                              where ps == null
                                              select p).ToArray();

            if (parsedNotUsedFeatures.Any())
            {
                yield return new ParserReportMessage($"Заголовки, которые можно использовать: {string.Join(", ", parsedNotUsedFeatures)}", ParserReportMessage.GreenColor);
            }
        }

        private IEnumerable<Dictionary<string, object>> GetMappedFeatures(
            IReadOnlyCollection<YandexMarketProductFeatureDto> yandexFeatures,
            Dictionary<string, string> featureMap,
            Dictionary<string, IEnumerable<string>> productNamesMap)
        {
            var yandexProducts = yandexFeatures.GroupBy(x => x.ProductId)
                .Select(x => new { ProductId = x.Key, Features = x.ToDictionary(y => y.FeatureName, y => y.FeatureValue) });

            foreach (var yandexProduct in yandexProducts)
            {
                TokenSource.Token.ThrowIfCancellationRequested();

                Dictionary<string, object> mappedProduct = new Dictionary<string, object>();

                foreach (KeyValuePair<string, string> featureMapping in featureMap)
                {
                    string mappedFeatureValue = ProductDescriptionBuilder.Build(featureMapping.Value, yandexProduct.Features);

                    if (!string.IsNullOrWhiteSpace(mappedFeatureValue))
                    {
                        mappedProduct[featureMapping.Key] = mappedFeatureValue;
                    }
                }

                if (productNamesMap.TryGetValue(yandexProduct.ProductId, out IEnumerable<string> productNames))
                {
                    foreach (string productName in productNames)
                    {
                        Dictionary<string, object> resultMappedProduct = mappedProduct.ToDictionary(x => x.Key, x => x.Value);

                        resultMappedProduct[nameof(ProductContentDto.Name)] = productName;

                        yield return resultMappedProduct;
                    }
                }
            }
        }

        private static IEnumerable<string> GetAllMatches(MatchCollection matches)
        {
            foreach (Match match in matches)
            {
                if (match.Groups.Count > 1)
                {
                    for (int i = 1; i < match.Groups.Count; i++)
                    {
                        foreach (Capture capture in match.Groups[i].Captures)
                        {
                            yield return capture.Value;
                        }
                    }
                }
            }
        }
    }
}