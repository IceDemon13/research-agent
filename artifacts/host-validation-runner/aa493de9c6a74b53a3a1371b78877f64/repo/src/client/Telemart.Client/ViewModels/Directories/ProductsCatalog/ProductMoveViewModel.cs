using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using DevExpress.Mvvm;
using DevExpress.Mvvm.DataAnnotations;
using DevExpress.Mvvm.Native;
using Microsoft.Extensions.Logging;
using Telemart.Client.Business.Audit;
using Telemart.Client.Business.Audit.Property;
using Telemart.Client.Common.MvvmEnhancements;
using Telemart.Client.Common.Services;
using Telemart.Client.Common.Utils;
using Telemart.Client.Core.Exceptions;
using Telemart.Client.Data.Requests.Features.Category;
using Telemart.Client.Data.Requests.Features.Employee;
using Telemart.Client.Data.Requests.Features.Products;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.Properties;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.Catalog;
using Telemart.Client.ViewModels.Base;
using Telemart.Client.ViewModels.Common.Product;
using Telemart.Client.ViewModels.Directories.Category;
using Telemart.Client.ViewModels.Validation;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.ViewModels.Directories.ProductsCatalog
{
    public sealed class ProductMoveViewModel : TelemartDialogViewModelBase
    {
        private const int UseProductValueId = 1;
        private const int UseCategoryValueId = 2;

        private readonly IAuditEntryPropertyProcessor defaultPropertyProcessor = new DefaultAuditEntryPropertyProcessor();

        private ProductCardViewItem productCard;
        private IReadOnlyDictionary<string, IAuditEntryPropertyProcessor> propertyProcessors;

        public ProductMoveViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            IMapper mapper)
            : base(webClient, dictionaries, messageFacadeService)
        {
            Mapper = mapper;

            HandleTargetCategoryChangedCommand = new AsyncCommand(HandleTargetCategoryChangedAsync);
            HandleProductPropertyChangedCommand = new DelegateCommand(HandleProductPropertyChanged);

            Properties = new ObservableRangeCollection<ProductPropertyViewItem>();
            Validations = new ObservableRangeCollection<ValidationResultItem>();
        }

        public ProductMoveViewModel()
        {
        }

        #region Commands

        public IAsyncCommand HandleTargetCategoryChangedCommand { get; }

        public IDelegateCommand HandleProductPropertyChangedCommand { get; }

        #endregion

        public string CurrentCategory
        {
            get { return GetProperty(() => CurrentCategory); }
            private set { SetProperty(() => CurrentCategory, value); }
        }

        public int CurrentCategoryId
        {
            get { return GetProperty(() => CurrentCategoryId); }
            private set { SetProperty(() => CurrentCategoryId, value); }
        }

        public ReadOnlyObservableCollection<CategoryViewItem> Categories
        {
            get { return GetProperty(() => Categories); }
            private set { SetProperty(() => Categories, value); }
        }

        public CategoryViewItem TargetCategory
        {
            get { return GetProperty(() => TargetCategory); }
            set { SetProperty(() => TargetCategory, value); }
        }

        public ObservableRangeCollection<ProductMoveViewModel.ProductPropertyViewItem> Properties
        {
            get { return GetProperty(() => Properties); }
            private set { SetProperty(() => Properties, value); }
        }

        public ObservableRangeCollection<ValidationResultItem> Validations
        {
            get { return GetProperty(() => Validations); }
            private set { SetProperty(() => Validations, value); }
        }

        public Result<ProductCardDto> Result { get; private set; }

        #region DialogSettings

        public override int Height => 800;

        public override int MinHeight => 400;

        public override int MinWidth => 450;

        public override int Width => 600;

        #endregion

        private IMapper Mapper { get; }

        public static void BuildMetadata(MetadataBuilder<ProductMoveViewModel> builder)
        {
            builder.Property(x => x.TargetCategory)
                .Required(() => Resources.RequiredErrorMessage)
                .MatchesInstanceRule(
                    (x, y) => x == null || x.Id != y.CurrentCategoryId,
                    () => "Товар уже находится в этой категории")
                .MatchesInstanceRule(
                    (x, y) => x == null || x.ParentLevel >= 0,
                    () => "Запрещено переносить товар в категорию выше родительской");
        }

        protected override void OnParameterChanged(object parameter)
        {
            if (IsInDesignMode)
            {
                return;
            }

            base.OnParameterChanged(parameter);

            productCard = (ProductCardViewItem)parameter;

            CurrentCategory = productCard.Category.NameFull;
            CurrentCategoryId = productCard.Category.Id;
        }

        protected override async Task HandleLoadedAsync()
        {
            List<CategoryDto> categories = await WebClient.ExecuteApiRequestAsync(new QueryCategories(), true).GetPagedResultDataAsync();

            Categories = categories
                .OrderBy(x => x.Left)
                .Select(x => Mapper.Map<CategoryViewItem>(x))
                .ToReadOnlyObservableCollection();

            List<EmployeeDto> employeesList = await WebClient.ExecuteApiRequestAsync(new QueryEmployees(), true).GetPagedResultDataAsync();

            Dictionary<int, string> employees = employeesList.ToDictionary(x => x.Id, x => x.Name);
            Dictionary<int, string> roboModes = Dictionaries.GetItems<PriceRobotMode>().ToDictionary(x => x.Id, x => x.Name);
            Dictionary<int, string> taxRates = Dictionaries.GetItems<TaxRate>().ToDictionary(x => x.Id, x => x.Name);
            Dictionary<int, string> warranties = Dictionaries.GetItems<Warranty>().ToDictionary(x => x.Id, x => x.Name);
            Dictionary<int, string> usdCurrencies = new Dictionary<int, string>
            {
                [UsdCurrency.Minus.Id] = UsdCurrency.Minus.Title,
                [UsdCurrency.Plus.Id] = UsdCurrency.Plus.Title
            };

            propertyProcessors = new Dictionary<string, IAuditEntryPropertyProcessor>(StringComparer.Ordinal)
            {
                ["manufactor"] = new TitleAuditEntryPropertyProcessor(Resources.CategoryEditingData_Main_Manufactor),
                ["id_robot_mode_manual"] = new IntDictionaryAuditEntryPropertyProcessor(Resources.CategoryEditingData_Robot_RobotModeManual, roboModes),
                ["id_robot_mode_auto"] = new IntDictionaryAuditEntryPropertyProcessor(Resources.CategoryEditingData_Robot_RobotModeAuto, roboModes),
                ["hotline"] = new TitleAuditEntryPropertyProcessor(Resources.CategoryEditingData_TradingPlaces_Hotline),
                ["id_employee"] = new IntDictionaryAuditEntryPropertyProcessor(Resources.CategoryEditingData_Properties_EmployeeId, employees),
                ["id_employee_sup"] = new IntDictionaryAuditEntryPropertyProcessor(Resources.CategoryEditingData_Properties_EmployeeSupId, employees),
                ["id_tax_rate"] = new IntDictionaryAuditEntryPropertyProcessor(Resources.CategoryEditingData_TaxRate, taxRates),
                ["kind"] = new TitleAuditEntryPropertyProcessor(Resources.CategoryEditingData_Properties_Kind),
                ["currency"] = new TitleAuditEntryPropertyProcessor(Resources.CategoryEditingData_Properties_Currency),
                ["usd_currency"] = new IntDictionaryAuditEntryPropertyProcessor(Resources.CategoryEditingData_Properties_UsdCurrency, usdCurrencies),
                ["id_warranty_retail"] = new IntDictionaryAuditEntryPropertyProcessor(Resources.CategoryEditingData_Properties_WarrantyRetail, warranties),
                ["id_warranty_wholesale"] = new IntDictionaryAuditEntryPropertyProcessor(Resources.CategoryEditingData_Properties_WarrantyWholesale, warranties),
                ["print_wcard"] = new TitleAuditEntryPropertyProcessor(Resources.CategoryEditingData_Properties_PrintWarrantyCard),
                ["keep_serial"] = new TitleAuditEntryPropertyProcessor(Resources.CategoryEditingData_Properties_KeepSerial),
                ["weight_est"] = new TitleAuditEntryPropertyProcessor(Resources.CategoryEditingData_Properties_Weight),
                ["free_delivery"] = new TitleAuditEntryPropertyProcessor(Resources.CategoryEditingData_Properties_FreeDelivery),
                ["self_barcode"] = new TitleAuditEntryPropertyProcessor(Resources.CategoryEditingData_Properties_SelfBarcode),
                ["prefix_rus"] = new TitleAuditEntryPropertyProcessor(Resources.CategoryEditingData_Prefix_PrefixRus),
                ["prefix_ukr"] = new TitleAuditEntryPropertyProcessor(Resources.CategoryEditingData_Prefix_PrefixUkr),
            };

            Title = "Перенос товара";
        }

        protected override bool CanOk()
        {
            return TargetCategory != null && Properties.Any() && !Validations.Any(x => x.IsError);
        }

        protected override async Task HandleOkAsync()
        {
            try
            {
                ProductMoveDto dto = new ProductMoveDto
                {
                    Id = productCard.ProductId,
                    CategoryId = TargetCategory.Id,
                    Properties = Properties
                        .Select(x => new ProductMovePropertyDto { Name = x.Name, OverrideFromCategory = x.Value.Id == UseCategoryValueId })
                        .ToArray()
                };

                MoveProduct request = new MoveProduct(dto);

                Result = await WebClient.ExecuteApiRequestAsync(request);

                if (Result.Warnings.Any())
                {
                    MessageFacadeService.ShowNotificationWarning("Товар перенесен с предупреждениями");
                    ShowValidationResultView("Предупреждения", Result.Warnings.Select(x => new ValidationResultItem(x, false)).ToList());
                }
                else
                {
                    MessageFacadeService.ShowNotificationInfo("Товар перенесен успешно");
                }

                IsOk = true;
                Close();
            }
            catch (UnexpectedSatusException exception)
            {
                MessageFacadeService.ShowNotificationError(Resources.ErrorExecutingOperation);
                ShowValidationResultView("Ошибки при выполнении операции", exception.GetErrorItems());
            }
            catch (UnexpectedErrorException exception)
            {
                Logger.LogError(exception, "Failed to move product");
                ShowValidationResultView(Resources.ServerConnectError, new[] { new ValidationResultItem(Resources.ServerUnavailable, true) });
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Failed to move product");
                MessageFacadeService.ShowNotificationError(Resources.ErrorExecutingOperation);
            }
        }

        private async Task HandleTargetCategoryChangedAsync()
        {
            Properties.Clear();
            Validations.Clear();

            if (TargetCategory == null)
            {
                return;
            }

            if (CurrentCategoryId == TargetCategory.Id)
            {
                MessageFacadeService.ShowNotificationWarning($"Товар уже находится в категории \"{TargetCategory.NameFull}\"");
                return;
            }

            if (TargetCategory.ParentLevel < 0)
            {
                MessageFacadeService.ShowNotificationWarning("Запрещено переносить товар в категорию выше родительской");
                return;
            }

            try
            {
                QueryProductMoveData request = new QueryProductMoveData(productCard.ProductId, TargetCategory.Id);
                ProductMovingDto response = await WebClient.ExecuteApiRequestAsync(request);

                Properties.AddRange(response.Properties.Select(MapProperty));
                Validations.AddRange(response.Validations.Select(x => new ValidationResultItem(x.Message, x.IsError)));
            }
            catch
            {
                MessageFacadeService.ShowNotificationError(Resources.ErrorDuringDataLoading);
            }
        }

        private void HandleProductPropertyChanged()
        {
            if (HandleTargetCategoryChangedCommand.IsExecuting)
            {
                return;
            }

            const string CurrencyPropertiesChangedWarning = "Цены в валютах обновятся не сразу";

            ProductPropertyViewItem[] currencyProperties = Properties
                .Where(x => x.Name.Equals("currency", StringComparison.Ordinal) || x.Name.Equals("usd_currency", StringComparison.Ordinal))
                .ToArray();

            if (currencyProperties.Any(x => x.IsChanged))
            {
                if (!Validations.Any(x => x.Message.Equals(CurrencyPropertiesChangedWarning, StringComparison.Ordinal)))
                {
                    Validations.Add(new ValidationResultItem(CurrencyPropertiesChangedWarning, false));
                }
            }
            else
            {
                ValidationResultItem validation = Validations.FirstOrDefault(x => x.Message.Equals(CurrencyPropertiesChangedWarning, StringComparison.Ordinal));

                if (validation != null)
                {
                    Validations.Remove(validation);
                }
            }
        }

        private ProductPropertyViewItem MapProperty(ProductMovingPropertyDto source)
        {
            return MapProperty(source, new ProductPropertyViewItem());
        }

        private ProductPropertyViewItem MapProperty(ProductMovingPropertyDto source, ProductPropertyViewItem target)
        {
            IAuditEntryPropertyProcessor propertyProcessor = propertyProcessors.GetValueOrDefault(source.Name, defaultPropertyProcessor);

            string productValue = source.ProductValue?.ToString() ?? string.Empty;
            string categoryValue = source.CategoryValue?.ToString() ?? string.Empty;

            AuditEntryProperty productPropertyValue = propertyProcessor.Process(source.Name, productValue, productValue, null);
            AuditEntryProperty categoryPropertyValue = propertyProcessor.Process(source.Name, categoryValue, categoryValue, null);

            target.Name = source.Name;
            target.DisplayName = productPropertyValue.PropertyName;
            target.ProductValue = productPropertyValue.NewValue;
            target.CategoryValue = categoryPropertyValue.NewValue;
            target.Values = new ObservableCollection<ComboBoxItem>(GetValues());
            target.Value = target.Values.First();

            return target;

            IEnumerable<ComboBoxItem> GetValues()
            {
                yield return new ComboBoxItem(UseProductValueId, productPropertyValue.NewValue);

                if (!string.Equals(productPropertyValue.NewValue, categoryPropertyValue.NewValue, StringComparison.Ordinal))
                {
                    yield return new ComboBoxItem(UseCategoryValueId, categoryPropertyValue.NewValue);
                }
            }
        }

        public class ProductPropertyViewItem : BindableBase
        {
            public string Name
            {
                get { return GetProperty(() => Name); }
                set { SetProperty(() => Name, value); }
            }

            public string DisplayName
            {
                get { return GetProperty(() => DisplayName); }
                set { SetProperty(() => DisplayName, value); }
            }

            public string ProductValue
            {
                get { return GetProperty(() => ProductValue); }
                set { SetProperty(() => ProductValue, value, () => { RaisePropertyChanged(nameof(IsChanged)); }); }
            }

            public string CategoryValue
            {
                get { return GetProperty(() => CategoryValue); }
                set { SetProperty(() => CategoryValue, value, () => { RaisePropertyChanged(nameof(IsChanged)); }); }
            }

            public ObservableCollection<ComboBoxItem> Values
            {
                get { return GetProperty(() => Values); }
                set { SetProperty(() => Values, value); }
            }

            public ComboBoxItem Value
            {
                get { return GetProperty(() => Value); }
                set { SetProperty(() => Value, value, () => { RaisePropertyChanged(nameof(IsChanged)); }); }
            }

            public bool IsChanged => Value.Id == UseCategoryValueId && !string.Equals(ProductValue, CategoryValue, StringComparison.Ordinal);

            public override string ToString()
            {
                return $"{nameof(Name)}: {Name}, {nameof(ProductValue)}: {ProductValue}, {nameof(CategoryValue)}: {CategoryValue}, {nameof(Value)}: {Value}, {nameof(IsChanged)}: {IsChanged}";
            }
        }
    }
}