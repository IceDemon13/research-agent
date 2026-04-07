using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using Telemart.Client.Common;
using Telemart.Client.Common.MvvmEnhancements;
using Telemart.Client.Common.Services;
using Telemart.Client.Common.Utils;
using Telemart.Client.Data.Requests.Features.AdditionalService;
using Telemart.Client.Data.Requests.Features.AdditionalService.Actions;
using Telemart.Client.Data.Requests.Features.Category;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.TransferObjects;
using Telemart.Client.ViewModels.Base;
using Telemart.Client.ViewModels.Dialogs;
using Telemart.Client.ViewModels.Nomenclature;
using Telemart.Client.ViewModels.Validation;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.ViewModels.AdditionalService
{
    public sealed class AdditionalServiceViewModel : TelemartEditorViewModelBase<AdditionalServiceDto, AdditionalServiceParameter, AdditionalServiceViewItem>
    {
        public AdditionalServiceViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            IMapper mapper,
            IMessenger messenger)
            : base(webClient, dictionaries, messageFacadeService, mapper, messenger)
        {
            AddSlaveCategoryCommand = new DelegateCommand(AddSlaveCategory);
            AddSlaveProductCommand = new DelegateCommand(AddSlaveProduct);
            AddProductProvideRuleCommand = new DelegateCommand(AddProductProvideRule);
            AddCategoryProvideRuleCommand = new DelegateCommand(AddCategoryProvideRule);
            DeleteSlaveCategoryCommand = new DelegateCommand<AdditionalServiceSlaveCategoryViewItem>(DeleteSlaveCategory, x => x != null);
            DeleteProvideRuleCommand = new DelegateCommand<AdditionalServiceProvideRuleItem>(DeleteProvideRule, x => x != null);
            SelectProductCommand = new DelegateCommand(SelectProduct);

            ProductTypes = new ObservableRangeCollection<ProductType>();
        }

        public AdditionalServiceViewModel()
        {
        }

        public IDelegateCommand AddSlaveCategoryCommand { get; }

        public IDelegateCommand AddSlaveProductCommand { get; }

        public IDelegateCommand DeleteSlaveCategoryCommand { get; }

        public IDelegateCommand AddProductProvideRuleCommand { get; }

        public IDelegateCommand AddCategoryProvideRuleCommand { get; }

        public IDelegateCommand DeleteProvideRuleCommand { get; }

        public IDelegateCommand SelectProductCommand { get; }

        public AdditionalServiceSlaveCategoryViewItem SelectedSlaveCategory
        {
            get { return GetProperty(() => SelectedSlaveCategory); }
            set { SetProperty(() => SelectedSlaveCategory, value); }
        }

        public AdditionalServiceProvideRuleItem SelectedProvideRule
        {
            get { return GetProperty(() => SelectedProvideRule); }
            set { SetProperty(() => SelectedProvideRule, value); }
        }

        public ObservableRangeCollection<ProductType> ProductTypes
        {
            get { return GetProperty(() => ProductTypes); }
            private set { SetProperty(() => ProductTypes, value); }
        }

        public ObservableCollection<AdditionalServicePriorityType> PriorityTypes
        {
            get { return GetProperty(() => PriorityTypes); }
            set { SetProperty(() => PriorityTypes, value); }
        }

        public ReadOnlyObservableCollection<ComboBoxItem> Categories
        {
            get { return GetProperty(() => Categories); }
            private set { SetProperty(() => Categories, value); }
        }

        public ReadOnlyObservableCollection<ProductPriceKind> Prices
        {
            get { return GetProperty(() => Prices); }
            private set { SetProperty(() => Prices, value); }
        }

        public bool AllowSetAssemblyPart
        {
            get { return GetProperty(() => AllowSetAssemblyPart); }
            private set { SetProperty(() => AllowSetAssemblyPart, value); }
        }

        public bool AllowSetControlInMovements
        {
            get { return GetProperty(() => AllowSetControlInMovements); }
            private set { SetProperty(() => AllowSetControlInMovements, value); }
        }

        public override int MinHeight => 590;

        public override int Height => 645;

        public override int MaxHeight => 1080;

        public override int MinWidth => 710;

        public override int Width => 710;

        public override int MaxWidth => 1920;

        protected override string CreatedActionMessage => "создана";

        protected override string EntityName => "Услуга";

        protected override string UpdatedActionMessage => "соxранена";

        protected override void SetCreateTitle()
        {
            Title = "Создание услуги";
        }

        protected override void SetEditTitle()
        {
            Title = "Услуга";
        }

        protected override async Task HandleLoadedAsync()
        {
            List<CategoryDto> categories = await WebClient.ExecuteApiRequestAsync(new QueryCategories(), true).GetPagedResultDataAsync();

            Categories = categories
               .Select(x => new ComboBoxItem(x.Id, x.Name))
               .ToReadOnlyObservableCollection();

            IReadOnlyCollection<ProductType> productTypes = Dictionaries.GetItems<ProductType>();

            ProductTypes.Add(productTypes.First(x => x.Id == ProductType.ServiceCertificateId));
            ProductTypes.Add(productTypes.First(x => x.Id == ProductType.ServiceId));
            ProductTypes.Add(productTypes.First(x => x.Id == ProductType.CertificateId));
            ProductTypes.Add(productTypes.First(x => x.Id == ProductType.AccessoryId));

            PriorityTypes = Dictionaries.GetItems<AdditionalServicePriorityType>().ToObservableCollection();

            Prices = Dictionaries.GetItems<ProductPriceKind>()
                .Where(x => x.Real && !x.Configurator)
                .ToReadOnlyObservableCollection();

            await base.HandleLoadedAsync();
        }

        protected override Task<bool> SaveAsync()
        {
            if (!Model.SlaveCategories.Any())
            {
                MessageFacadeService.ShowNotificationWarning("Должна быть добавлена как минимум одна категория или товар");
                return Task.FromResult(false);
            }

            return base.SaveAsync();
        }

        protected override void OnModelPropertyChangedInternal(object sender, PropertyChangedEventArgs e)
        {
            base.OnModelPropertyChangedInternal(sender, e);

            switch (e.PropertyName)
            {
                case nameof(AdditionalServiceViewItem.ProductTypeId):

                    if (Model.ProductTypeId == ProductType.CertificateId
                        || Model.ProductTypeId == ProductType.ServiceCertificateId
                        || Model.ProductTypeId == ProductType.AccessoryId)
                    {
                        AllowSetAssemblyPart = true;
                    }
                    else
                    {
                        AllowSetAssemblyPart = false;
                        Model.AssemblyPart = false;
                    }

                    if (Model.ProductTypeId == ProductType.ServiceCertificateId || Model.ProductTypeId == ProductType.ServiceId)
                    {
                        AllowSetControlInMovements = true;
                    }
                    else
                    {
                        AllowSetControlInMovements = false;
                        Model.ControlInMovements = false;
                    }

                    break;
            }
        }

        protected override void AfterSetData()
        {
            base.AfterSetData();

            if (Model == null)
            {
                return;
            }

            if (Model.ProductTypeId == ProductType.CertificateId
                        || Model.ProductTypeId == ProductType.ServiceCertificateId
                        || Model.ProductTypeId == ProductType.AccessoryId)
            {
                AllowSetAssemblyPart = true;
            }
            else
            {
                AllowSetAssemblyPart = false;
                Model.AssemblyPart = false;
            }

            if (Model.ProductTypeId == ProductType.ServiceCertificateId || Model.ProductTypeId == ProductType.ServiceId)
            {
                AllowSetControlInMovements = true;
            }
            else
            {
                AllowSetControlInMovements = false;
                Model.ControlInMovements = false;
            }

            RaisePropertyChanged(nameof(Model.ProductTypeId));
        }

        protected override Task<Result<AdditionalServiceDto>> CreateEntityAsync()
        {
            AdditionalServiceCreateDto createDto = new AdditionalServiceCreateDto()
            {
                Name = Model.Name,
                NameUa = Model.NameUa,
                NameEn = Model.NameEn,
                Description = Model.Description,
                DescriptionUa = Model.DescriptionUa,
                DescriptionEn = Model.DescriptionEn,
                Active = Model.Active,
                AdditionalWarranty = Model.AdditionalWarranty,
                AutoAdd = Model.AutoAdd,
                GroupId = Model.GroupId,
                ProductId = Model.ProductId,
                MinPrice = Model.MinPrice,
                ControlInMovements = Model.ControlInMovements,
                Percent = Model.Percent,
                ProductTypeId = Model.ProductTypeId!.Value,
                PriorityTypeId = Model.PriorityTypeId!.Value,
                PresenceOfCustomer = Model.PresenceOfCustomer,
                IsLocal = Model.IsLocal,
                RequireProductsToProvide = Model.RequireProductsToProvide,
                AssemblyPart = Model.AssemblyPart,
                CreateDiscount = Model.CreateDiscount,
                Disassembly = Model.Disassembly,
                PriceIds = Model.PriceIds.ToArray(),
                SlaveCategories = Model.SlaveCategories.Select(x => new AdditionalServiceSlaveCategoryCreateDto(x.CategoryId, x.ProductId, x.FeatureId, x.FeatureValueId)).ToList(),
                ProvideRules = Model.ProvideRules.Select(x => new AdditionalServiceProvideRuleCreateDto(x.CategoryId, x.ProductId, x.FeatureId, x.FeatureValueId)).ToList()
            };

            CreateAdditionalService gatewayRequest = new CreateAdditionalService(createDto);
            return WebClient.ExecuteApiRequestAsync(gatewayRequest);
        }

        protected override Task<AdditionalServiceDto> GetEntityAsync(int id)
        {
            return WebClient.ExecuteApiRequestAsync(new QueryAdditionalService(id));
        }

        protected override Task<LockResponse<AdditionalServiceDto>> LockEntityAsync(int id)
        {
            return WebClient.ExecuteApiRequestAsync(new LockAdditionalService(id));
        }

        protected override Task<LockResponse<AdditionalServiceDto>> UnlockEntityAsync(int id)
        {
            return WebClient.ExecuteApiRequestAsync(new UnlockAdditionalService(id));
        }

        protected override Task<Result<AdditionalServiceDto>> UpdateEntityAsync()
        {
            AdditionalServiceSaveDto saveDto = new AdditionalServiceSaveDto()
            {
                Active = Model.Active,
                AdditionalWarranty = Model.AdditionalWarranty,
                AutoAdd = Model.AutoAdd,
                ControlInMovements = Model.ControlInMovements,
                ProductId = Model.ProductId,
                Name = Model.Name,
                NameUa = Model.NameUa,
                NameEn = Model.NameEn,
                Description = Model.Description,
                DescriptionUa = Model.DescriptionUa,
                DescriptionEn = Model.DescriptionEn,
                GroupId = Model.GroupId,
                Id = Model.Id,
                ProductTypeId = Model.ProductTypeId!.Value,
                PriorityTypeId = Model.PriorityTypeId!.Value,
                MinPrice = Model.MinPrice,
                Percent = Model.Percent,
                PresenceOfCustomer = Model.PresenceOfCustomer,
                IsLocal = Model.IsLocal,
                RequireProductsToProvide = Model.RequireProductsToProvide,
                AssemblyPart = Model.AssemblyPart,
                CreateDiscount = Model.CreateDiscount,
                Disassembly = Model.Disassembly,
                PriceIds = Model.PriceIds.ToArray(),
                SlaveCategories = Model.SlaveCategories.Select(x => new AdditionalServiceSlaveCategorySaveDto(x.Id, x.CategoryId, x.ProductId, x.FeatureId, x.FeatureValueId)).ToArray(),
                ProvideRules = Model.ProvideRules.Select(x => new AdditionalServiceProvideRuleSaveDto(x.Id, x.CategoryId, x.ProductId, x.FeatureId, x.FeatureValueId, x.Active)).ToArray(),
            };

            return WebClient.ExecuteApiRequestAsync(new UpdateAdditionalService(Model.Id, saveDto));
        }

        private void AddSlaveCategory()
        {
            GetCategoryFeatureValueViewModel viewModel = DialogDocumentManagerService.ShowView<GetCategoryFeatureValueViewModel>(new GetCategoryFeatureValueParameter(false, false), this);

            if (!viewModel.IsOk)
            {
                return;
            }

            if (viewModel.SelectedFeatureValue is null && Model.SlaveCategories.Where(x => !x.FeatureValueId.HasValue).FirstOrDefault(x => x.CategoryId == viewModel.SelectedCategory.Id) != null)
            {
                MessageFacadeService.ShowNotificationWarning("Категория уже добавлена");
                return;
            }

            if (Model.SlaveCategories.FirstOrDefault(x => x.CategoryId == viewModel.SelectedCategory.Id && x.FeatureId == viewModel.SelectedFeature?.Id && x.FeatureValueId == viewModel.SelectedFeatureValue?.Id) != null)
            {
                MessageFacadeService.ShowNotificationWarning("Значение характеристики уже добавлено");
                return;
            }

            string categoryName = Categories.FirstOrDefault(x => x.Id == viewModel.SelectedCategory.Id).DisplayValue;

            Model.SlaveCategories.Add(
                new AdditionalServiceSlaveCategoryViewItem
                {
                    CategoryId = viewModel.SelectedCategory.Id,
                    CategoryName = categoryName,
                    Feature = viewModel.SelectedFeature?.DisplayValue,
                    FeatureId = viewModel.SelectedFeature?.Id,
                    FeatureValue = viewModel.SelectedFeatureValue?.DisplayValue,
                    FeatureValueId = viewModel.SelectedFeatureValue?.Id
                });

            MessageFacadeService.ShowNotificationInfo("Категория успешно добавлена");
        }

        private void AddSlaveProduct()
        {
            NomenclatureViewModel model = ShowNomenclatureViewModel();

            if (!model.IsOk)
            {
                return;
            }

            List<string> alreadyAddedProducts = new List<string>();

            foreach (NomenclatureViewItem newProduct in model.GetSelectedItems())
            {
                if (Model.SlaveCategories.Where(x => x.ProductId.HasValue).Select(x => x.ProductId).Contains(newProduct.Id))
                {
                    alreadyAddedProducts.Add(newProduct.Name);
                }
                else
                {
                    Model.SlaveCategories.Add(new AdditionalServiceSlaveCategoryViewItem { ProductId = newProduct.Id, ProductName = newProduct.Name });
                }
            }

            if (alreadyAddedProducts.Any())
            {
                MessageFacadeService.ShowValidationResultView("Ранее добавленные товары", alreadyAddedProducts.Select(x => new ValidationResultItem(x, false)), this);
            }
            else
            {
                MessageFacadeService.ShowNotificationInfo("Товар успешно добавлен");
            }
        }

        private void AddProductProvideRule()
        {
            NomenclatureViewModel model = ShowNomenclatureViewModel();

            if (!model.IsOk)
            {
                return;
            }

            List<string> alreadyAddedProducts = new List<string>();

            foreach (NomenclatureViewItem newProduct in model.GetSelectedItems())
            {
                if (Model.ProvideRules.Where(x => x.ProductId.HasValue).Select(x => x.ProductId).Contains(newProduct.Id))
                {
                    alreadyAddedProducts.Add(newProduct.Name);
                }
                else
                {
                    Model.ProvideRules.Add(new AdditionalServiceProvideRuleItem { ProductId = newProduct.Id, ProductName = newProduct.Name, Active = true });
                }
            }

            if (alreadyAddedProducts.Any())
            {
                MessageFacadeService.ShowValidationResultView("Ранее добавленные товары", alreadyAddedProducts.Select(x => new ValidationResultItem(x, false)), this);
            }
            else
            {
                MessageFacadeService.ShowNotificationInfo("Товар успешно добавлен");
            }
        }

        private void AddCategoryProvideRule()
        {
            GetCategoryFeatureValueViewModel viewModel = DialogDocumentManagerService.ShowView<GetCategoryFeatureValueViewModel>(new GetCategoryFeatureValueParameter(false, false), this);

            if (!viewModel.IsOk)
            {
                return;
            }

            if (Model.ProvideRules.Where(x => !x.FeatureValueId.HasValue).FirstOrDefault(x => x.CategoryId == viewModel.SelectedCategory.Id) != null)
            {
                MessageFacadeService.ShowNotificationWarning("Категория уже добавлена");
                return;
            }

            if (Model.ProvideRules.FirstOrDefault(x => x.CategoryId == viewModel.SelectedCategory.Id && x.FeatureId == viewModel.SelectedFeature?.Id && x.FeatureValueId == viewModel.SelectedFeatureValue?.Id) != null)
            {
                MessageFacadeService.ShowNotificationWarning("Значение характеристики уже добавлено");
                return;
            }

            string categoryName = Categories.FirstOrDefault(x => x.Id == viewModel.SelectedCategory.Id).DisplayValue;

            Model.ProvideRules.Add(new AdditionalServiceProvideRuleItem
                {
                    CategoryId = viewModel.SelectedCategory.Id,
                    CategoryName = categoryName,
                    Feature = viewModel.SelectedFeature?.DisplayValue,
                    FeatureId = viewModel.SelectedFeature?.Id,
                    FeatureValue = viewModel.SelectedFeatureValue?.DisplayValue,
                    FeatureValueId = viewModel.SelectedFeatureValue?.Id,
                    Active = true
                });

            MessageFacadeService.ShowNotificationInfo("Категория успешно добавлена");
        }

        private void DeleteSlaveCategory(AdditionalServiceSlaveCategoryViewItem item)
        {
            Model.SlaveCategories.Remove(item);
        }

        private void DeleteProvideRule(AdditionalServiceProvideRuleItem item)
        {
            Model.ProvideRules.Remove(item);
        }

        private void SelectProduct()
        {
            NomenclatureViewOptions options = new NomenclatureViewOptions(
                NomenclatureViewPriceContext.Client,
                Constants.TelemartContractorId,
                NomenclatureViewSelectionMode.Single,
                false);

            NomenclatureViewModel nomenclatureViewModel = SizeableDialogDocumentManagerService.ShowView<NomenclatureViewModel>(options, this);

            if (nomenclatureViewModel.IsOk)
            {
                NomenclatureViewItem product = nomenclatureViewModel.GetSelectedItems().First();

                Model.ProductId = product.Id;
                Model.ProductName = product.GetLocalName(LocalizableNameType.Ukr);
            }
        }

        private NomenclatureViewModel ShowNomenclatureViewModel()
        {
            NomenclatureViewOptions options = new NomenclatureViewOptions(
                NomenclatureViewPriceContext.Client,
                Constants.TelemartContractorId,
                NomenclatureViewSelectionMode.ByCheck,
                false);

            NomenclatureViewModel nomenclatureViewModel = SizeableDialogDocumentManagerService.ShowView<NomenclatureViewModel>(options, this);

            return nomenclatureViewModel;
        }
    }
}