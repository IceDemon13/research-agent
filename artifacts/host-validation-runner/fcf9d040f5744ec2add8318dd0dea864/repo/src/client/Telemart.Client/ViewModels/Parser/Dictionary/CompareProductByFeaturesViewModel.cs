using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using DevExpress.Mvvm;
using DevExpress.Mvvm.DataAnnotations;
using DevExpress.Mvvm.Native;
using DevExpress.Xpf.Core;
using Telemart.Client.Common.ErrorHandler;
using Telemart.Client.Common.MvvmEnhancements;
using Telemart.Client.Common.Services;
using Telemart.Client.Data.Requests.Features.Contractor;
using Telemart.Client.Data.Requests.Features.ParserSearchTemplate;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.Paging;
using Telemart.Client.TransferObjects.ParserSearchTemplate;
using Telemart.Client.ViewModels.Base;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.ViewModels.Parser.Dictionary
{
    public sealed class CompareProductByFeaturesViewModel : TelemartDialogViewModelBase
    {
        private readonly IMapper mapper;
        private readonly IErrorHandler errorHandler;

        public CompareProductByFeaturesViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            IErrorHandler errorHandler,
            IMapper mapper)
            : base(webClient, dictionaries, messageFacadeService)
        {
            this.mapper = mapper;
            this.errorHandler = errorHandler;

            FindCommand = new AsyncCommand(FindAsync, () => SelectedTemplates?.Any() == true);
            SaveCommand = new AsyncCommand<bool>(SaveAsync);
        }

        public IAsyncCommand FindCommand { get; }

        public IAsyncCommand SaveCommand { get; }

        #region DialogSettings

        public override int Width => 1380;

        public override int MinWidth => 720;

        public override int MaxWidth => 1920;

        public override int Height => 720;

        public override int MinHeight => 700;

        public override int MaxHeight => 1080;

        #endregion

        public ReadOnlyObservableCollection<ParserSearchTemplateViewItem> AllTemplates
        {
            get { return GetProperty(() => AllTemplates); }
            private set { SetProperty(() => AllTemplates, value); }
        }

        public ObservableCollection<int> SelectedTemplates
        {
            get { return GetProperty(() => SelectedTemplates); }
            set { SetProperty(() => SelectedTemplates, value); }
        }

        public ReadOnlyObservableCollection<ProductSearchTemplateViewItem> Products
        {
            get { return GetProperty(() => Products); }
            private set { SetProperty(() => Products, value); }
        }

        public ProductSearchTemplateViewItem SelectedProduct
        {
            get { return GetProperty(() => SelectedProduct); }
            set { SetProperty(() => SelectedProduct, value, () => RaisePropertyChanged(nameof(SelectedProduct.Features))); }
        }

        public ReadOnlyObservableCollection<ComboBoxItem> Contractors
        {
            get { return GetProperty(() => Contractors); }
            private set { SetProperty(() => Contractors, value); }
        }

        public static void BuildMetadata(MetadataBuilder<CompareProductByFeaturesViewModel> builder)
        {
            builder.Property(x => x.SelectedTemplates)
                .MatchesInstanceRule((x, y) => x?.Any() == true, () => "Должен быть выбран минимум 1 шаблон");
        }

        protected override async Task HandleLoadedAsync()
        {
            List<ParserSearchTemplateDto> templates = await WebClient.ExecuteApiRequestAsync(new QueryParserSearchTemplatesRequest());

            AllTemplates = templates
                .Select(x => new ParserSearchTemplateViewItem { Id = x.Id, Name = x.Name })
                .ToReadOnlyObservableCollection();

            PagedResult<ContractorDto> contractors = await WebClient.ExecuteApiRequestAsync(new QueryContractors(), true);

            Contractors = contractors.Data.Select(x => new ComboBoxItem(x.Id, x.Name)).ToReadOnlyObservableCollection();

            await base.HandleLoadedAsync();

            Title = "Сопоставление по характеристикам";
        }

        protected override async Task HandleOkAsync()
        {
            bool success = await SaveAsync(true);

            if (success)
            {
                CloseOk();
            }
        }

        private async Task<bool> SaveAsync(bool ignoreCompareHelper)
        {
            bool success = false;

            ProductSearchTemplateSaveItemDto[] saveItems = Products
                .Select(x => new ProductSearchTemplateSaveItemDto(x.ParserSearchTemplateId, x.ParserAliasId, x.ContractorId, x.ProductId, x.Active))
                .ToArray();

            ProductSearchtemplateSaveDto saveDto = new(saveItems, SelectedTemplates);

            (await errorHandler.HandleErrorsAsync(ct => WebClient.ExecuteApiRequestAsync(new SaveProductSearchTemplates(saveDto)), "сохранении сопоставлений", "Сопоставления сохранены", this, true))
                .IfNotNull(_ =>
                {
                    success = true;
                });

            return success;
        }

        private async Task FindAsync()
        {
            SplashScreenManager splashScreenManager = SplashScreenManager.CreateWaitIndicator();
            splashScreenManager.Show();

            Result<IReadOnlyCollection<ProductSearchTemplateDto>> productTemplatesResult = await errorHandler.HandleErrorsAsync(ct => WebClient.ExecuteApiRequestAsync(new SearchProductsByTemplates(SelectedTemplates)), "поиске товаров", "Товары найдены", this, true);

            splashScreenManager.Close();

            if (productTemplatesResult == null)
            {
                return;
            }

            Products = productTemplatesResult.Data.Select(x => mapper.Map<ProductSearchTemplateViewItem>(x)).ToReadOnlyObservableCollection();

            RaisePropertyChanged(nameof(SelectedProduct.Features));
        }
    }
}