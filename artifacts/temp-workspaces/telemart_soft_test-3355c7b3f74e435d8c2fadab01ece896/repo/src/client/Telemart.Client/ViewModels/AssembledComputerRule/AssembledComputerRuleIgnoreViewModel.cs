using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using DevExpress.Mvvm.Native;
using Telemart.Client.Common.ErrorHandler;
using Telemart.Client.Common.MvvmEnhancements;
using Telemart.Client.Common.Services;
using Telemart.Client.Data.Requests.Features.Category;
using Telemart.Client.Data.Requests.Features.Content;
using Telemart.Client.Data.Requests.Features.SlotHosts;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.Content;
using Telemart.Client.ViewModels.Base;
using Telemart.Client.ViewModels.Content.ProductFeatureGroups;

namespace Telemart.Client.ViewModels.AssembledComputerRule
{
    public sealed class AssembledComputerRuleIgnoreViewModel : TelemartDialogViewModelBase
    {
        private IReadOnlyCollection<int> _ignoreSlotConsumerIds;

        public AssembledComputerRuleIgnoreViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            IErrorHandler errorHandler)
            : base(webClient, dictionaries, messageFacadeService)
        {
            ErrorHandler = errorHandler;
        }

        public ReadOnlyCollection<AssemblySlotHostConsumerItem> AssemblySlotHostConsumers
        {
            get { return GetProperty(() => AssemblySlotHostConsumers); }
            set { SetProperty(() => AssemblySlotHostConsumers, value); }
        }

        public AssemblySlotHostConsumerItem SelectedAssemblySlotHostConsumer
        {
            get { return GetProperty(() => SelectedAssemblySlotHostConsumer); }
            set { SetProperty(() => SelectedAssemblySlotHostConsumer, value); }
        }

        public ReadOnlyObservableCollection<ComboBoxItem> Categories
        {
            get { return GetProperty(() => Categories); }
            private set { SetProperty(() => Categories, value); }
        }

        public ReadOnlyObservableCollection<ComboBoxItem> Features
        {
            get { return GetProperty(() => Features); }
            private set { SetProperty(() => Features, value); }
        }

        public override int MinHeight => 550;

        public override int Height => 550;

        public override int MaxHeight => 1080;

        public override int MinWidth => 500;

        public override int Width => 500;

        public override int MaxWidth => 1920;

        private IErrorHandler ErrorHandler { get; }

        public IReadOnlyCollection<int> GetIgnoreSlotConsumerIds()
        {
            return _ignoreSlotConsumerIds;
        }

        protected override async Task HandleLoadedAsync()
        {
            Title = "Игнорировать правила пересчетов";

            AssembledComputerRuleIgnoreParameter parameter = (AssembledComputerRuleIgnoreParameter)Parameter;

            _ignoreSlotConsumerIds = parameter.IgnoreSlotConsumerIds?.ToArray() ?? Array.Empty<int>();

            await Task.WhenAll(LoadSlotHostsAsync(), LoadCategoriesAsync());

            await LoadFeaturesAsync();
        }

        protected override Task HandleOkAsync()
        {
            _ignoreSlotConsumerIds = AssemblySlotHostConsumers.Where(x => x.Ignore)
                .Select(x => x.Id)
                .ToArray();

            CloseOk();

            return Task.CompletedTask;
        }

        private async Task LoadSlotHostsAsync()
        {
            List<AssemblySlotHostDto> resultAssemblySlotHostDtos = await ErrorHandler.HandleErrorsAsync(
                _ => WebClient.ExecuteApiRequestAsync(new QueryAssemblySlotHosts()),
                "получении правил конфигурации",
                null,
                this,
                true,
                showNotification: false);

            AssemblySlotHostConsumers = MapAssemblySlotHostConsumerItem(resultAssemblySlotHostDtos).ToReadOnlyCollection();
        }

        private async Task LoadCategoriesAsync()
        {
            List<CategoryDto> resultCategories = await ErrorHandler.HandleErrorsAsync(
                _ => WebClient.ExecuteApiRequestAsync(new QueryCategories(), true).GetPagedResultDataAsync(),
                "получении катигорий",
                null,
                this,
                true,
                showNotification: false);

            Categories = resultCategories
                .Select(x => new ComboBoxItem(x.Id, x.Name))
                .ToReadOnlyObservableCollection();
        }

        private async Task LoadFeaturesAsync()
        {
            List<int> categoryIds = AssemblySlotHostConsumers.Select(x => x.CategoryId).Distinct().ToList();

            List<FeatureFullDto> featureFullDtos = new List<FeatureFullDto>();

            foreach (int categoryId in categoryIds)
            {
                if (categoryId > 0)
                {
                    FeatureFilteringItem filteringItem = new FeatureFilteringItem(categoryId);

                    List<FeatureFullDto> features = await ErrorHandler.HandleErrorsAsync(
                        _ => WebClient.ExecuteApiRequestAsync(new QueryFeatures(filteringItem)).GetPagedResultDataAsync(),
                        "получении характеристик",
                        null,
                        this,
                        true,
                        showNotification: false);

                    featureFullDtos.AddRange(features);
                }
            }

            Features = featureFullDtos.Select(x => new ComboBoxItem(x.Id, x.Name)).ToReadOnlyObservableCollection();
        }

        private IReadOnlyCollection<AssemblySlotHostConsumerItem> MapAssemblySlotHostConsumerItem(IReadOnlyCollection<AssemblySlotHostDto> assemblySlotHostDtos)
        {
            List<AssemblySlotHostConsumerItem> allItems = new List<AssemblySlotHostConsumerItem>();

            foreach (AssemblySlotHostDto assemblySlotHostDto in assemblySlotHostDtos)
            {
                List<AssemblySlotHostConsumerItem> consumerItems = assemblySlotHostDto.Consumers.Select(x => Map(
                    assemblySlotHostDto.CategoryId,
                    assemblySlotHostDto.FeatureId,
                    assemblySlotHostDto.ValidationMessageTemplateRu,
                    x,
                    _ignoreSlotConsumerIds))
                    .ToList();

                allItems.AddRange(consumerItems);
            }

            return allItems;
        }

        private AssemblySlotHostConsumerItem Map(
            int slotHostCatigoryId,
            int slotHostFeatureId,
            string message,
            AssemblySlotConsumerDto assemblySlotConsumerDto,
            IReadOnlyCollection<int> ignoreConsumerIds)
        {
            return new AssemblySlotHostConsumerItem()
            {
                CategoryId = slotHostCatigoryId,
                FeatureId = slotHostFeatureId,
                ValidationMessageTemplate = message,
                ConsumerCategoryId = assemblySlotConsumerDto.CategoryId,
                ConsumerFeatureId = assemblySlotConsumerDto.FeatureId,
                Id = assemblySlotConsumerDto.Id,
                Ignore = ignoreConsumerIds.Contains(assemblySlotConsumerDto.Id)
            };
        }
    }
}