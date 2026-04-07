using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using AutoMapper;
using DevExpress.Mvvm;
using DevExpress.Mvvm.DataAnnotations;
using DevExpress.Mvvm.Native;
using DevExpress.Xpf.Core;
using Microsoft.Extensions.Logging;
using Telemart.Client.Common;
using Telemart.Client.Common.ErrorHandler;
using Telemart.Client.Common.Messages;
using Telemart.Client.Common.MvvmEnhancements;
using Telemart.Client.Common.Services;
using Telemart.Client.Core.Exceptions;
using Telemart.Client.Core.Helpers;
using Telemart.Client.Data.Options;
using Telemart.Client.Data.Requests.Features.City;
using Telemart.Client.Data.Requests.Features.Contractor;
using Telemart.Client.Data.Requests.Features.Contractor.Actions;
using Telemart.Client.Data.Requests.Features.Contractor.Carry;
using Telemart.Client.Data.Requests.Features.Contractor.Contact;
using Telemart.Client.Data.Requests.Features.Contractor.SupplierCategoryAbc;
using Telemart.Client.Data.Requests.Features.Contractor.Warehouse;
using Telemart.Client.Data.Requests.Features.Country;
using Telemart.Client.Data.Requests.Features.Employee;
using Telemart.Client.Data.Requests.Features.Invoice;
using Telemart.Client.Data.Requests.Features.OwnershipForms;
using Telemart.Client.Data.Requests.Features.Warehouse;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Data.WebClient.Security;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.Properties;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.City;
using Telemart.Client.TransferObjects.Country;
using Telemart.Client.TransferObjects.ParserRequests;
using Telemart.Client.TransferObjects.Warehouse;
using Telemart.Client.ViewModels.Base;
using Telemart.Client.ViewModels.Cities;
using Telemart.Client.ViewModels.Common;
using Telemart.Client.ViewModels.Dialogs;
using Telemart.Client.ViewModels.Directories.Contractor.ParserSettings;
using Telemart.Client.ViewModels.Validation;
using Telemart.Client.WebClient.Jobs;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.ViewModels.Directories.Contractor
{
    public sealed class ContractorViewModel : TelemartEditorViewModelBase<ContractorDto, ContractorViewMessage, ContractorViewItem>
    {
        private readonly TelegramBotOptions _telegramBotOptions;
        private bool _canEdit;
        private Dictionary<int, string> _owhershipFormNames;
        private List<ContractorCityItem> _allCities;

        public ContractorViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            IMapper mapper,
            IMessenger messenger,
            IErrorHandler errorHandler,
            TelegramBotOptions telegramBotOptions,
            ContractorTemplatesEditorViewModel templatesEditor,
            IParserClient parserClient)
            : base(webClient, dictionaries, messageFacadeService, mapper, messenger)
        {
            _telegramBotOptions = telegramBotOptions;
            TemplatesEditor = templatesEditor;
            ErrorHandler = errorHandler;
            ParserClient = parserClient;

            RefreshSupplierCarriesCommand = new AsyncCommand(RefreshSupplierCarriesAsync);
            AddSupplierCarryCommand = new DelegateCommand(AddSupplierCarry, () => CanCreateSupplierCarry);
            EditSupplierCarryCommand = new DelegateCommand<SupplierCarryViewItem>(EditSupplierCarry, x => x != null && CanUpdateSupplierCarry);
            RemoveSupplierCarryCommand = new AsyncCommand<SupplierCarryViewItem>(RemoveSupplierCarryAsync, x => x != null && CanDeleteSupplierCarry);
            SetAsMainCarryCommand = new AsyncCommand<SupplierCarryViewItem>(SetAsMainCarryAsync, x => x != null && !x.Main && CanUpdateSupplierCarry);
            SetAsNotMainCarryCommand = new AsyncCommand<SupplierCarryViewItem>(SetAsNotMainCarryAsync, x => x != null && x.Main && CanUpdateSupplierCarry);

            DeleteCurrencyPermissionCommand = new DelegateCommand(DeleteCurrencyPermission, () => SelectedCurrencyPermission is not null);
            AddCurrencyPermissionCommand = new DelegateCommand(AddCurrencyPermission);
            SelectedCurrencyPermissionChangedCommand = new DelegateCommand(SelectedCurrencyPermissionChanged);

            RefreshContactsCommand = new AsyncCommand(RefreshContactsAsync);
            AddContractorContactCommand = new DelegateCommand(AddContractorContact, () => CanCreateContact);
            CreateTelegramChatCommand = new DelegateCommand(CreateTelegramChat);
            EditContractorContactCommand = new DelegateCommand<ContractorContactViewItem>(EditContractorContact, x => x != null && CanUpdateContact);
            RemoveContractorContactCommand = new AsyncCommand<ContractorContactViewItem>(RemoveContractorContactAsync, x => x != null && CanDeleteContact);

            RefreshSupplierWarehousesCommand = new AsyncCommand(RefreshSupplierWarehousesAsync);
            AddWarehouseCommand = new DelegateCommand(AddWarehouse, () => CanCreateSupplierWarehouse);
            EditWarehouseCommand = new AsyncCommand<SupplierWarehouseViewItem>(EditWarehouseAsync, x => x != null && CanUpdateSupplierWarehouse);
            RemoveWarehouseCommand = new AsyncCommand<SupplierWarehouseViewItem>(RemoveWarehouseAsync, x => x != null && CanDeleteSupplierWarehouse);
            DeactivateWarehousesCommand = new AsyncCommand(DeactivateWarehousesAsync, () => SupplierWarehouses?.Any(y => y.Active) == true && CanDeactivateSupplierWarehouses);

            EditContractorClassCommand = new AsyncCommand(EditContractorClassAsync, () => CanEditAbc);
            CreateOrganizationCommand = new DelegateCommand(CreateOrganization, () => WebClient.IsOperationAllowed(BusinessOperation.ContractorOrganizationCreate));

            EditParserSettingsCommand = new DelegateCommand(EditParserSettings, () => SelectedParserSettings is not null && CanEditParserSettings);
            AddParserSettingsCommand = new DelegateCommand(AddParserSettings);

            RefreshSupplierCategoriesAbcCommand = new AsyncCommand(RefreshSupplierCategoriesAbcAsync);
            CreateSupplierCategoryAbcCommand = new AsyncCommand(CreateSupplierCategoryAbcAsync, () => CanCreateSupplierCategory);
            EditSupplierCategoryAbcCommand = new AsyncCommand<SupplierCategoryAbcViewItem>(EditSupplierCategoryAbcAsync, x => x != null && CanUpdateSupplierCategory);
            DeleteSupplierCategoryAbcCommand = new AsyncCommand<SupplierCategoryAbcViewItem>(DeleteSupplierCategoryAbcAsync, x => x != null && CanDeleteSupplierCategory);
            SwitchParseFeaturesCommand = new AsyncCommand(SwitchParseFeaturesAsync);
            SetParseFeaturesPriorityCommand = new AsyncCommand(SetParseFeaturesPriorityAsync, () => Model?.ParseFeatures == true);
            CreateForeignCityCommand = new DelegateCommand(CreateForeignCity, () => Model?.CountryId != null && Model?.CountryId != Constants.UkraineCountryId);

            HandlePreviewKeyDownCommand = new DelegateCommand<KeyEventArgs>(HandlePreviewKeyDown);
            HandleSelectionChangedCommand = new DelegateCommand<ValueChangedEventArgs<FrameworkElement>>(HandleSelectionChanged);

            Messenger.Register<ParserSettingsMessage>(this, OnParserSettingsMessage);

            Messenger.Register<ForeignCityMessage>(this, OnForeignCityMessage);
        }

        public ContractorViewModel()
        {
        }

        #region Commands

        public IDelegateCommand AddContractorContactCommand { get; }

        public IAsyncCommand SwitchParseFeaturesCommand { get; }

        public IAsyncCommand SetParseFeaturesPriorityCommand { get;  }

        public IDelegateCommand CreateOrganizationCommand { get; }

        public IDelegateCommand EditContractorContactCommand { get; }

        public IDelegateCommand AddCurrencyPermissionCommand { get; }

        public IDelegateCommand DeleteCurrencyPermissionCommand { get; }

        public IDelegateCommand SelectedCurrencyPermissionChangedCommand { get; }

        public IAsyncCommand RemoveContractorContactCommand { get; }

        public IDelegateCommand AddSupplierCarryCommand { get; }

        public IDelegateCommand EditSupplierCarryCommand { get; }

        public IAsyncCommand RemoveSupplierCarryCommand { get; }

        public IAsyncCommand RemoveWarehouseCommand { get; }

        public IAsyncCommand DeactivateWarehousesCommand { get; }

        public IAsyncCommand EditWarehouseCommand { get; }

        public IDelegateCommand AddWarehouseCommand { get; }

        public IDelegateCommand HandlePreviewKeyDownCommand { get; }

        public IDelegateCommand HandleSelectionChangedCommand { get; }

        public IAsyncCommand RefreshContactsCommand { get; }

        public IDelegateCommand CreateTelegramChatCommand { get; }

        public IAsyncCommand RefreshSupplierWarehousesCommand { get; }

        public IAsyncCommand RefreshSupplierCarriesCommand { get; }

        public IAsyncCommand EditContractorClassCommand { get; }

        public IAsyncCommand RefreshSupplierCategoriesAbcCommand { get; }

        public IAsyncCommand CreateSupplierCategoryAbcCommand { get; }

        public IAsyncCommand EditSupplierCategoryAbcCommand { get; }

        public IAsyncCommand DeleteSupplierCategoryAbcCommand { get; }

        public IDelegateCommand EditParserSettingsCommand { get; }

        public IDelegateCommand AddParserSettingsCommand { get; }

        public IAsyncCommand SetAsMainCarryCommand { get; }

        public IAsyncCommand SetAsNotMainCarryCommand { get; }

        public IDelegateCommand CreateForeignCityCommand { get; }

        #endregion

        #region INPC

        public ReadOnlyObservableCollection<ContractorCityItem> Cities
        {
            get { return GetProperty(() => Cities); }
            private set { SetProperty(() => Cities, value); }
        }

        public ReadOnlyObservableCollection<CountryDto> Countries
        {
            get { return GetProperty(() => Countries); }
            private set { SetProperty(() => Countries, value); }
        }

        public ObservableCollection<string> Organizations
        {
            get { return GetProperty(() => Organizations); }
            private set { SetProperty(() => Organizations, value); }
        }

        public ReadOnlyObservableCollection<ContractorContactPositionDto> Positions
        {
            get { return GetProperty(() => Positions); }
            private set { SetProperty(() => Positions, value); }
        }

        public ReadOnlyObservableCollection<ContractorPopupViewItem> Contractors
        {
            get { return GetProperty(() => Contractors); }
            private set { SetProperty(() => Contractors, value); }
        }

        public ReadOnlyObservableCollection<ComboBoxItem> Employees
        {
            get { return GetProperty(() => Employees); }
            private set { SetProperty(() => Employees, value); }
        }

        public ReadOnlyObservableCollection<Subdivision> Subdivisions
        {
            get { return GetProperty(() => Subdivisions); }
            private set { SetProperty(() => Subdivisions, value); }
        }

        public ReadOnlyObservableCollection<WarehouseDto> Warehouses
        {
            get { return GetProperty(() => Warehouses); }
            private set { SetProperty(() => Warehouses, value); }
        }

        public ObservableCollection<SupplierCarryViewItem> Carries
        {
            get { return GetProperty(() => Carries); }
            private set { SetProperty(() => Carries, value); }
        }

        public ReadOnlyObservableCollection<ProductPriceKind> PriceTypes
        {
            get { return GetProperty(() => PriceTypes); }
            private set { SetProperty(() => PriceTypes, value); }
        }

        public ReadOnlyObservableCollection<ComboBoxItem> OwnershipForms
        {
            get { return GetProperty(() => OwnershipForms); }
            private set { SetProperty(() => OwnershipForms, value); }
        }

        public ObservableCollection<ContractorContactViewItem> Contacts
        {
            get { return GetProperty(() => Contacts); }
            private set { SetProperty(() => Contacts, value); }
        }

        public ObservableCollection<SupplierWarehouseViewItem> SupplierWarehouses
        {
            get { return GetProperty(() => SupplierWarehouses); }
            private set { SetProperty(() => SupplierWarehouses, value); }
        }

        public ObservableCollection<SupplierCategoryAbcViewItem> SupplierCategories
        {
            get { return GetProperty(() => SupplierCategories); }
            private set { SetProperty(() => SupplierCategories, value); }
        }

        public ObservableCollection<AbcType> AbcClasses
        {
            get { return GetProperty(() => AbcClasses); }
            private set { SetProperty(() => AbcClasses, value); }
        }

        public ReadOnlyObservableCollection<ComboBoxItem> Currencies
        {
            get { return GetProperty(() => Currencies); }
            private set { SetProperty(() => Currencies, value); }
        }

        public ReadOnlyObservableCollection<ComboBoxItem> ParserSettingsTypes
        {
            get { return GetProperty(() => ParserSettingsTypes); }
            private set { SetProperty(() => ParserSettingsTypes, value); }
        }

        public ReadOnlyObservableCollection<SupplierInvoiceProcessorDto> SupplierInvoiceProcessors
        {
            get { return GetProperty(() => SupplierInvoiceProcessors); }
            private set { SetProperty(() => SupplierInvoiceProcessors, value); }
        }

        public SupplierInvoiceProcessorDto SelectedSupplierInvoiceProcessor
        {
            get { return GetProperty(() => SelectedSupplierInvoiceProcessor); }
            set { SetProperty(() => SelectedSupplierInvoiceProcessor, value, SelectedSupplierInvoiceProcessorChanged); }
        }

        public ContractorCurrencyPermissionViewItem SelectedCurrencyPermission
        {
            get { return GetProperty(() => SelectedCurrencyPermission); }
            set { SetProperty(() => SelectedCurrencyPermission, value, SelectedSupplierInvoiceProcessorChanged); }
        }

        public ParserSettingsViewItem SelectedParserSettings
        {
            get { return GetProperty(() => SelectedParserSettings); }
            set { SetProperty(() => SelectedParserSettings, value); }
        }

        public bool IsHelpVisible
        {
            get { return GetProperty(() => IsHelpVisible); }
            private set { SetProperty(() => IsHelpVisible, value); }
        }

        public int SelectedTabIndex
        {
            get { return GetProperty(() => SelectedTabIndex); }
            set { SetProperty(() => SelectedTabIndex, value); }
        }

        public string FullName => Model?.OwnershipFormId == null || _owhershipFormNames == null
            ? string.Empty
            : $"{_owhershipFormNames[Model.OwnershipFormId.Value]} \"{Model.Name}\"";

        #region Permissions

        public bool CanEditAbc
        {
            get { return GetProperty(() => CanEditAbc); }
            private set { SetProperty(() => CanEditAbc, value); }
        }

        public bool CanEditParserSettings
        {
            get { return GetProperty(() => CanEditParserSettings); }
            private set { SetProperty(() => CanEditParserSettings, value); }
        }

        public bool CanDeleteSupplierCarry
        {
            get { return GetProperty(() => CanDeleteSupplierCarry); }
            private set { SetProperty(() => CanDeleteSupplierCarry, value); }
        }

        public bool CanUpdateSupplierCarry
        {
            get { return GetProperty(() => CanUpdateSupplierCarry); }
            private set { SetProperty(() => CanUpdateSupplierCarry, value); }
        }

        public bool CanCreateSupplierCarry
        {
            get { return GetProperty(() => CanCreateSupplierCarry); }
            private set { SetProperty(() => CanCreateSupplierCarry, value); }
        }

        public bool CanDeleteSupplierWarehouse
        {
            get { return GetProperty(() => CanDeleteSupplierWarehouse); }
            private set { SetProperty(() => CanDeleteSupplierWarehouse, value); }
        }

        public bool CanDeactivateSupplierWarehouses
        {
            get { return GetProperty(() => CanDeactivateSupplierWarehouses); }
            private set { SetProperty(() => CanDeactivateSupplierWarehouses, value); }
        }

        public bool CanUpdateSupplierWarehouse
        {
            get { return GetProperty(() => CanUpdateSupplierWarehouse); }
            private set { SetProperty(() => CanUpdateSupplierWarehouse, value); }
        }

        public bool CanCreateSupplierWarehouse
        {
            get { return GetProperty(() => CanCreateSupplierWarehouse); }
            private set { SetProperty(() => CanCreateSupplierWarehouse, value); }
        }

        public bool CanDeleteContact
        {
            get { return GetProperty(() => CanDeleteContact); }
            private set { SetProperty(() => CanDeleteContact, value); }
        }

        public bool CanUpdateContact
        {
            get { return GetProperty(() => CanUpdateContact); }
            private set { SetProperty(() => CanUpdateContact, value); }
        }

        public bool CanCreateContact
        {
            get { return GetProperty(() => CanCreateContact); }
            private set { SetProperty(() => CanCreateContact, value); }
        }

        public bool CanDeleteSupplierCategory
        {
            get { return GetProperty(() => CanDeleteSupplierCategory); }
            private set { SetProperty(() => CanDeleteSupplierCategory, value); }
        }

        public bool CanUpdateSupplierCategory
        {
            get { return GetProperty(() => CanUpdateSupplierCategory); }
            private set { SetProperty(() => CanUpdateSupplierCategory, value); }
        }

        public bool CanCreateSupplierCategory
        {
            get { return GetProperty(() => CanCreateSupplierCategory); }
            private set { SetProperty(() => CanCreateSupplierCategory, value); }
        }

        public bool CanEditPurchase
        {
            get { return GetProperty(() => CanEditPurchase); }
            private set { SetProperty(() => CanEditPurchase, value); }
        }

        public bool CanChangeInBuh1C
        {
            get { return GetProperty(() => CanEditAbc); }
            private set { SetProperty(() => CanEditAbc, value); }
        }

        #endregion

        #endregion INPC

        #region DialogSettings

        public override int Height => 680;

        public override int MinHeight => 680;

        public override int MinWidth => 640;

        public override int Width => 640;

        #endregion

        public ContractorTemplatesEditorViewModel TemplatesEditor { get; }

        protected override string CreatedActionMessage => throw new NotSupportedException();

        protected override string EntityName => Model.TypeDisplayValue;

        protected override string UpdatedActionMessage => $"сохранен{(Model.IsFolder ? "а" : string.Empty)}";

        private IErrorHandler ErrorHandler { get; }

        private IParserClient ParserClient { get; }

        public static void BuildMetadata(MetadataBuilder<ContractorViewModel> builder)
        {
            builder.Property(x => x.SelectedSupplierInvoiceProcessor)
                .MatchesInstanceRule((x, y) => (string.IsNullOrWhiteSpace(y.Model?.PurchaseLogin) && string.IsNullOrWhiteSpace(y.Model?.PurchasePassword)) || x != null, () => Resources.RequiredErrorMessage);
        }

        protected override Task<Result<ContractorDto>> CreateEntityAsync()
        {
            throw new NotSupportedException();
        }

        protected override void SetEditTitle()
        {
            Title = $"{Model.TypeDisplayValue} \"{Model.Name}\" ({Model.Id})";
        }

        protected override Task<Result<ContractorDto>> UpdateEntityAsync()
        {
            ContractorSaveDto contractorSaveDto = new ContractorSaveDto
            {
                Id = Model.Id,
                EmployeeId = Model.EmployeeId!.Value,
                CountryId = Model.CountryId,
                Comment = Model.Comment,
                ParentId = Model.ParentId,
                Name = Model.Name,
                Url = Model.Url,
                SubdivisionId = Model.Subdivision.Id,
                Active = Model.Active,
                PriceTypeId = Model.PriceTypeId,
                IsSupplier = Model.IsSupplier,
                AutoSource = Model.AutoSource,
                IsServiceSupplier = Model.IsServiceSupplier,
                Limit = Model.Limit,
                Edrpou = Model.Edrpou,
                Discount = Model.Discount,
                IsIndividual = Model.IsIndividual,
                CurrencyManual = Model.CurrencyManual,
                OldClient = Model.OldClient,
                OwnershipFormId = Model.OwnershipFormId,
                Tin = Model.Tin,
                ActualAddress = Model.ActualAddress,
                LegalAddress = Model.LegalAddress,
                VatAllowed = Model.VatAllowed,
                AllowDocuments = Model.AllowDocuments,
                RetailWarranty = Model.RetailWarranty,
                Organization = Model.Organization,
                GracePeriod = Model.GracePeriod,
                ReturnPeriod = Model.ReturnPeriod,
                Buh1CId = Model.Buh1CId,
                CurrencyPermissions = Model.CurrencyPermissions.Select(x => new ContractorCurrencyPermissionDto()
                {
                    Id = x.Id,
                    CurrencyId = x.CurrencyId,
                    Sale = x.Sale,
                    Purchase = x.Purchase,
                    CurrencyControl = x.CurrencyControl
                }).ToArray()
            };

            if (Model.CountryId == null || Model.CountryId.Value == Constants.UkraineCountryId)
            {
                contractorSaveDto.CityId = Model.CityId > 0 ? Model.CityId.Value : null;
            }
            else
            {
                contractorSaveDto.ForeignCityId = Model.CityId > 0 ? Model.CityId.Value : null;
            }

            if (Model.IncludePurchaseData())
            {
                contractorSaveDto.Purchase = new ContractorPurchaseDto
                {
                    ProcessorName = SelectedSupplierInvoiceProcessor?.Name,
                    Login = Model.PurchaseLogin,
                    Password = Model.PurchasePassword,
                    AutoReserve = Model.PurchaseAutoReserve,
                    AutoPurchase = Model.PurchaseAutoPurchase,
                    CheckUnique = Model.PurchaseCheckUnique
                };
            }

            return WebClient.ExecuteApiRequestAsync(new UpdateContractor(contractorSaveDto));
        }

        protected override object CreateEntityMessage(ContractorDto dto, MessageType messageType)
        {
            return new ContractorMessage(dto, messageType);
        }

        protected override Task<ContractorDto> GetEntityAsync(int id)
        {
            return WebClient.ExecuteApiRequestAsync(new QueryContractor(id));
        }

        protected override async Task HandleLoadedAsync()
        {
            _canEdit = WebClient.IsOperationAllowed(BusinessOperation.ContractorUpdate);
            CanEditAbc = WebClient.IsOperationAllowed(BusinessOperation.ContractorUpdateAbc);
            CanEditParserSettings = WebClient.IsOperationAllowed(BusinessOperation.ContractorUpdateParserSettings);

            CanCreateContact = WebClient.IsOperationAllowed(BusinessOperation.ContractorContactCreate);
            CanUpdateContact = WebClient.IsOperationAllowed(BusinessOperation.ContractorContactUpdate);
            CanDeleteContact = WebClient.IsOperationAllowed(BusinessOperation.ContractorContactDelete);

            CanCreateSupplierWarehouse = WebClient.IsOperationAllowed(BusinessOperation.ContractorSupplierWarehouseCreate);
            CanUpdateSupplierWarehouse = WebClient.IsOperationAllowed(BusinessOperation.ContractorSupplierWarehouseUpdate);
            CanDeleteSupplierWarehouse = WebClient.IsOperationAllowed(BusinessOperation.ContractorSupplierWarehouseDelete);
            CanDeactivateSupplierWarehouses = WebClient.IsOperationAllowed(BusinessOperation.ContractorSupplierWarehouseUpdate);

            CanCreateSupplierCarry = WebClient.IsOperationAllowed(BusinessOperation.ContractorSupplierCarryCreate);
            CanUpdateSupplierCarry = WebClient.IsOperationAllowed(BusinessOperation.ContractorSupplierCarryUpdate);
            CanDeleteSupplierCarry = WebClient.IsOperationAllowed(BusinessOperation.ContractorSupplierCarryDelete);

            CanCreateSupplierCategory = WebClient.IsOperationAllowed(BusinessOperation.ContractorSupplierCategoryCreate);
            CanUpdateSupplierCategory = WebClient.IsOperationAllowed(BusinessOperation.ContractorSupplierCategoryUpdate);
            CanDeleteSupplierCategory = WebClient.IsOperationAllowed(BusinessOperation.ContractorSupplierCategoryDelete);

            CanEditPurchase = WebClient.IsOperationAllowed(BusinessOperation.ContractorEditPurchase);
            CanChangeInBuh1C = WebClient.IsOperationAllowed(BusinessOperation.ContractorCreateInBuh1C);

            Subdivisions = Dictionaries.GetItems<Subdivision>().ToReadOnlyObservableCollection();
            PriceTypes = Dictionaries.GetItems<ProductPriceKind>().Where(x => x.Real).OrderBy(x => x.Name).ToReadOnlyObservableCollection();
            AbcClasses = Dictionaries.GetItems<AbcType>().ToObservableCollection();
            Currencies = Dictionaries.GetItems<Currency>().Select(x => new ComboBoxItem(x.Id, x.Name.ToUpperInvariant())).ToReadOnlyObservableCollection();
            ParserSettingsTypes = Dictionaries.GetItems<ParserSettingsType>().Select(x => new ComboBoxItem(x.Id, x.Name)).ToReadOnlyObservableCollection();

            await Task.WhenAll(
                RefreshCitiesAsync(),
                RefreshCountriesAsync(),
                RefreshContractorsAsync(),
                RefreshWarehousesAsync(),
                RefreshPositionsAsync(),
                RefreshSupplierInvoiceProcessorsAsync(),
                RefreshOwnershipFormsAsync());

            await base.HandleLoadedAsync();

            await RefreshEmployeesAsync();
            await RefreshSupplierWarehousesAsync();

            SetEmployees();

            RaisePropertyChanged(nameof(FullName));

            TemplatesEditor.SelectedContractor = Model;

            ContractorViewMessage parameter = (ContractorViewMessage)Parameter;

            SelectedTabIndex = 0;

            if (parameter.OpenOnLogisticsTab)
            {
                SelectedTabIndex = GetLogisticsTabIndex();
            }
        }

        protected override Task<LockResponse<ContractorDto>> LockEntityAsync(int id)
        {
            return WebClient.ExecuteApiRequestAsync(new LockContractor(id));
        }

        protected override Task<LockResponse<ContractorDto>> UnlockEntityAsync(int id)
        {
            return WebClient.ExecuteApiRequestAsync(new UnlockContractor(id));
        }

        protected override void SetCreateTitle()
        {
            throw new NotSupportedException();
        }

        protected override bool CanEdit()
        {
            return _canEdit;
        }

        protected override async Task<bool> SaveAsync()
        {
            if (Model.CurrencyPermissions.Any(x => IDataErrorInfoHelper.HasErrors(x)))
            {
                MessageFacadeService.ShowNotificationError("Валюты настроены не корректно");
                return false;
            }

            return await base.SaveAsync();
        }

        protected override IEnumerable<string> GetMembersToIgnore()
        {
            yield return nameof(ContractorViewItem.AbcType);
            yield return nameof(ContractorViewItem.ParseFeatures);
            yield return nameof(ContractorViewItem.ParseFeaturesPriority);
            yield return nameof(ContractorViewItem.ParseFeaturesPriorityStr);
        }

        protected override void AfterSetData()
        {
            base.AfterSetData();

            TemplatesEditor.SelectedContractor = Model;

            SelectedSupplierInvoiceProcessor = SupplierInvoiceProcessors?.FirstOrDefault(x => x.Name == Model.PurchaseProcessorName);

            SetEmployees();

            SelectedCountryChanged();
        }

        protected override void OnInitializeInDesignModeInternal()
        {
            Cities = new[] { new ContractorCityItem { Id = 23, Name = "Dnepr", CountryId = Constants.UkraineCountryId } }.ToReadOnlyObservableCollection();
            Contractors = new[] { new ContractorPopupViewItem(1, null, "Parent Test", true) }.ToReadOnlyObservableCollection();
            Employees = new[] { new ComboBoxItem(79, "Demo") }.ToReadOnlyObservableCollection();
            Subdivisions = new[] { Subdivision.Wholesale }.ToReadOnlyObservableCollection();
            PriceTypes = new[] { new ProductPriceKind(ProductPriceKind.Telemart1, "Цена1", false, 1, 1, true) }.ToReadOnlyObservableCollection();

            Model.Id = 23;
            Model.ParentId = 1;
            Model.Name = "Test test";
            Model.Subdivision = Subdivision.Wholesale;
            Model.IsRetail = false;
            Model.IsFolder = false;
            Model.IsCompetitor = false;
            Model.IsClient = true;
            Model.IsSupplier = true;
            Model.IsServiceSupplier = true;
            Model.Limit = 37;
            Model.CityId = 23;
            Model.EmployeeId = 79;
            Model.Employee = new ComboBoxItem(79, "Demo");
            Model.PriceTypeId = ProductPriceKind.Telemart1;
        }

        protected override void OnModelPropertyChangedInternal(object sender, PropertyChangedEventArgs e)
        {
            base.OnModelPropertyChangedInternal(sender, e);

            switch (e.PropertyName)
            {
                case nameof(Model.Name):
                case nameof(Model.OwnershipFormId):
                    RaisePropertyChanged(nameof(FullName));
                    break;

                case nameof(Model.PurchaseLogin):
                case nameof(Model.PurchasePassword):
                    RaisePropertyChanged(nameof(SelectedSupplierInvoiceProcessor));
                    break;
                case nameof(Model.CountryId):
                    SelectedCountryChanged();
                    break;
            }
        }

        private void AddSupplierCarry()
        {
            SupplierCarryViewModel viewModel = DialogDocumentManagerService.ShowView<SupplierCarryViewModel>(new SupplierCarryViewItem(), this);

            if (viewModel.IsOk)
            {
                Carries.Add(viewModel.Model);
            }
        }

        private void SetEmployees()
        {
            if (Employees != null)
            {
                ModelOriginal.Employee = Model.Employee = Employees.FirstOrDefault(x => x.Id == Model.EmployeeId);
            }
        }

        private void EditSupplierCarry(SupplierCarryViewItem viewItem)
        {
            SupplierCarryViewModel viewModel = DialogDocumentManagerService.ShowView<SupplierCarryViewModel>(viewItem.Clone(), this);

            if (!viewModel.IsOk)
            {
                return;
            }

            SupplierCarryViewItem changedItem = viewModel.Model;

            viewItem.CarryType = changedItem.CarryType;
            viewItem.SupplierWarehouseId = changedItem.SupplierWarehouseId;
            viewItem.WarehouseId = changedItem.WarehouseId;
            viewItem.Days = changedItem.Days;
            viewItem.Filial = changedItem.Filial;
            viewItem.TimeClose = changedItem.TimeClose;
            viewItem.TimeCloseSt = changedItem.TimeCloseSt;
            viewItem.TimeGet = changedItem.TimeGet;
            viewItem.TimeGetSt = changedItem.TimeGetSt;
            viewItem.TimeArrive = changedItem.TimeArrive;
            viewItem.TimeArriveSt = changedItem.TimeArriveSt;
        }

        private async Task RemoveSupplierCarryAsync(SupplierCarryViewItem obj)
        {
            if (!MessageFacadeService.Confirm("Вы уверены?"))
            {
                return;
            }

            try
            {
                await WebClient.ExecuteApiRequestAsync(new DeleteContractorCarry(Model.Id, obj.Id));
                Carries.Remove(obj);
                MessageFacadeService.ShowNotificationInfo("Доставка успешно удалена");
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Failed to delete supplier carry");
                MessageFacadeService.ShowNotificationError("Ошибка при удалении доставки");
            }
        }

        private void AddContractorContact()
        {
            ContractorContactViewModel viewModel = DialogDocumentManagerService.ShowView<ContractorContactViewModel>(
                ContractorContactViewItem.Create(),
                this);

            if (viewModel.IsOk)
            {
                RefreshContactsCommand.Execute(null);
            }
        }

        private void EditContractorContact(ContractorContactViewItem viewItem)
        {
            ContractorContactViewModel viewModel = DialogDocumentManagerService.ShowView<ContractorContactViewModel>(viewItem.Clone(), this);

            if (viewModel.IsOk)
            {
                RefreshContactsCommand.Execute(null);
            }
        }

        private void CreateTelegramChat()
        {
            if (!WebClient.IsOperationAllowed(BusinessOperation.ContractorCreateTelegramChat))
            {
                MessageFacadeService.ShowNotificationError("У вас нет прав на выполнение операции");
                return;
            }

            if (string.IsNullOrWhiteSpace(WebClient.AuthenticatedEmployee.Telegram))
            {
                MessageFacadeService.ShowNotificationError("У Вас не заполнен публичный аккаунт Telegram");
                return;
            }

            if (!string.IsNullOrWhiteSpace(Model.TelegramChatId))
            {
                MessageFacadeService.ShowNotificationError($"С контрагентом '{Model.Name}' уже создана группа в Telegram");
                return;
            }

            MessageBoxResult result = MessageFacadeService.ShowMessageBox(
                "\n 1. Создайте группу в telegram (если еще не создана)" +
                $"\n 2. В боте @{_telegramBotOptions.ContractorsBotName} нажмите на три точки и затем 'Добавить в группу'" +
                "\n 3. В списке выберите вашу группу" +
                "\n 4. Выполните команду /init [код контрагента]" +
                "\n 5. Добавьте в группу контактные лица (если еще не добавлены)" +
                "\n 6. Готово, бот инициализирован и готов отправлять сообщения!" +
                "\n\n\n Открыть бота после закрытия формы?",
                "Как создать группу с ботом для контрагента",
                MessageBoxButton.YesNo,
                MessageBoxImage.None,
                MessageBoxResult.Cancel);

            if (result == MessageBoxResult.Yes)
            {
                ProcessHelper.Start($"https://t.me/{_telegramBotOptions.ContractorsBotName}");
            }
        }

        private async Task RemoveContractorContactAsync(ContractorContactViewItem viewItem)
        {
            if (!MessageFacadeService.Confirm("Вы уверены?"))
            {
                return;
            }

            try
            {
                await WebClient.ExecuteApiRequestAsync(new DeleteContractorContact(Model.Id, viewItem.Id));

                MessageFacadeService.ShowNotificationInfo("Контакт успешно удален");

                RefreshContactsCommand.Execute(null);
                TemplatesEditor.HandleSelectedContractorChangedCommand.Execute(TemplatesEditor.SelectedContractor);
            }
            catch (UnexpectedSatusException exception)
            {
                Logger.LogError(exception, "Failed to delete contractor contact");
                MessageFacadeService.ShowNotificationError("Ошибка при удалении контакта");
                ShowValidationResultView("Ошибки при удалении контакта", exception.GetErrorItems());
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Failed to delete contractor contact");
                MessageFacadeService.ShowNotificationError("Ошибка при удалении контакта");
            }
        }

        private async Task SwitchParseFeaturesAsync()
        {
            if (!WebClient.IsOperationAllowed(BusinessOperation.ContractorUpdate))
            {
                MessageFacadeService.ShowNotificationError("У вас нет прав на выполнение операции");
                return;
            }

            await WebClient.ExecuteApiRequestAsync(new SwitchParseFeatures(Model.Id));

            Model.ParseFeatures = !Model.ParseFeatures;

            if (!Model.ParseFeatures)
            {
                Model.ParseFeaturesPriority = null;
            }
        }

        private async Task SetParseFeaturesPriorityAsync()
        {
            if (!WebClient.IsOperationAllowed(BusinessOperation.ContractorUpdate))
            {
                MessageFacadeService.ShowNotificationError("У вас нет прав на выполнение операции");
                return;
            }

            ChangeItemViewModel viewModel = DialogDocumentManagerService
                .ShowView<ChangeItemViewModel>(
                    new ChangeItemParameter(
                        Enumerable.Range(1, 100).Select(x => new ComboBoxItem(x, x.ToString())).ToReadOnlyObservableCollection(),
                        Model.ParseFeaturesPriority.ToString(),
                        "Изменение приоритета характеристик"),
                    this);

            if (!viewModel.IsOk)
            {
                return;
            }

            await WebClient.ExecuteApiRequestAsync(new SetParseFeaturesPriority(Model.Id, viewModel.NewItem.Value.Id));
            Model.ParseFeaturesPriority = viewModel.NewItem.Value.Id;
        }

        private void AddWarehouse()
        {
            SupplierWarehouseViewModel viewModel = DialogDocumentManagerService.ShowView<SupplierWarehouseViewModel>(new SupplierWarehouseViewItem { SupplierId = Model.Id }, this);

            if (viewModel.IsOk)
            {
                SupplierWarehouses.Add(viewModel.Model);
            }
        }

        private async Task EditWarehouseAsync(SupplierWarehouseViewItem warehouseViewItem)
        {
            SupplierWarehouseViewModel viewModel = DialogDocumentManagerService.ShowView<SupplierWarehouseViewModel>(warehouseViewItem.Clone(), this);

            if (!viewModel.IsOk)
            {
                return;
            }

            SupplierWarehouseViewItem changedSupplierWarehouse = viewModel.Model;

            warehouseViewItem.CityId = changedSupplierWarehouse.CityId;
            warehouseViewItem.Name = changedSupplierWarehouse.Name;
            warehouseViewItem.ShortName = changedSupplierWarehouse.ShortName;
            warehouseViewItem.Pricer24Id = changedSupplierWarehouse.Pricer24Id;
            warehouseViewItem.SupplierWarehouseAvails = changedSupplierWarehouse.SupplierWarehouseAvails;
            warehouseViewItem.Active = changedSupplierWarehouse.Active;

            if (warehouseViewItem.Active == false)
            {
                await ErrorHandler.HandleErrorsAsync(
                    _ => ParserClient.ClearAvailableBySupplierWarehouseIdAsync(new ClearAvailableBySupplierWarehouseIdRequest()
                    {
                        ContractorId = Model.Id
                    }),
                    "удалении наличия в складах",
                    null,
                    this,
                    false,
                    false);
            }
        }

        private async Task RemoveWarehouseAsync(SupplierWarehouseViewItem warehouseViewItem)
        {
            if (!MessageFacadeService.Confirm("Вы уверены?"))
            {
                return;
            }

            try
            {
                await WebClient.ExecuteApiRequestAsync(new DeleteContractorWarehouse(Model.Id, warehouseViewItem.Id));
                SupplierWarehouses.Remove(warehouseViewItem);
                MessageFacadeService.ShowNotificationInfo("Склад успешно удален");
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Failed to delete supplier warehouse");
                MessageFacadeService.ShowNotificationError("Ошибка при удалении склада");
            }
        }

        private async Task DeactivateWarehousesAsync()
        {
            Result result = await ErrorHandler.HandleErrorsAsync(
                x => WebClient.ExecuteApiRequestAsync(new DeactivateContractorsWarehouses(Model.Id)),
                "деакивации складов",
                "Склады деактивированы",
                this,
                true,
                false,
                confirmText: "Деактивировать все склады");

            if (result?.IsSuccess == true)
            {
                SupplierWarehouses.ForEach(x => x.Active = false);

                await ErrorHandler.HandleErrorsAsync(
                    _ => ParserClient.ClearAvailableBySupplierWarehouseIdAsync(new ClearAvailableBySupplierWarehouseIdRequest()
                    {
                        ContractorId = Model.Id
                    }),
                    "удалении наличия в складах",
                    null,
                    this,
                    false,
                    false);
            }
        }

        private async Task RefreshContractorsAsync()
        {
            List<ContractorDto> contractors = await WebClient.ExecuteApiRequestAsync(new QueryContractors()).GetPagedResultDataAsync();

            Organizations = contractors
                .Where(x => !string.IsNullOrWhiteSpace(x.Organization))
                .Select(x => x.Organization)
                .Distinct()
                .OrderBy(x => x)
                .ToObservableCollection();

            Contractors = contractors
                .Where(x => x.IsFolder)
                .Select(x => new ContractorPopupViewItem(x.Id, x.ParentId, x.Name, x.IsFolder))
                .OrderBy(x => x.Name)
                .ToReadOnlyObservableCollection();
        }

        private async Task RefreshCitiesAsync()
        {
            List<CityDto> cities = await WebClient.ExecuteApiRequestAsync(new QueryCities(), true).GetPagedResultDataAsync();

            List<CityDto> citiesList = new List<CityDto>(cities)
            {
                new CityDto { Id = -1, Name = Constants.UkraineDisplayValue, Position = -1 }
            };

            _allCities = citiesList
                .OrderBy(x => x.Position)
                .ThenBy(x => x.Name)
                .Select(x => new ContractorCityItem { Id = x.Id, Name = x.Name, CountryId = Constants.UkraineCountryId })
                .ToList();

            List<ForeignCityDto> foreignCitiesDto = await ErrorHandler.HandleErrorsAsync(
                _ => WebClient.ExecuteApiRequestAsync(new QueryForeignCities()),
                "получении списка инностранных городов",
                null,
                this,
                true,
                showNotification: false);

            if (foreignCitiesDto != null)
            {
                _allCities.AddRange(foreignCitiesDto.Select(x => new ContractorCityItem { Id = x.Id, Name = x.Name, CountryId = x.CountryId }));
            }
        }

        private async Task RefreshCountriesAsync()
        {
            List<CountryDto> countriesDto = await ErrorHandler.HandleErrorsAsync(
                _ => WebClient.ExecuteApiRequestAsync(new QueryCountries(), true),
                "получении списка стран",
                null,
                this,
                true,
                showNotification: false);

            if (countriesDto != null)
            {
                Countries = countriesDto.ToReadOnlyObservableCollection();
            }
        }

        private async Task RefreshEmployeesAsync()
        {
            List<EmployeeDto> employees = await WebClient.ExecuteApiRequestAsync(new QueryEmployees(), true).GetPagedResultDataAsync();

            Employees = employees
                .Where(x => x.Active || x.Id == Model.EmployeeId)
                .OrderBy(x => x.Name)
                .Select(x => new ComboBoxItem(x.Id, x.Name, x.Active))
                .ToReadOnlyObservableCollection();
        }

        private async Task RefreshPositionsAsync()
        {
            List<ContractorContactPositionDto> positions = await WebClient.ExecuteApiRequestAsync(new QueryContractorContactPositions(), true);
            Positions = positions.OrderBy(x => x.Position).ThenBy(x => x.Name).ToReadOnlyObservableCollection();
        }

        private async Task RefreshWarehousesAsync()
        {
            int[] warehouseTypeIds = new[] { WarehouseKind.Main.Id, WarehouseKind.Pickup.Id, WarehouseKind.ShowCase.Id, WarehouseKind.Assembly.Id };

            List<WarehouseDto> warehouses = await WebClient.ExecuteApiRequestAsync(new QueryWarehouses(), true).GetPagedResultDataAsync();
            Warehouses = warehouses.Where(x => warehouseTypeIds.Contains(x.TypeId)).OrderBy(x => x.Position).ToReadOnlyObservableCollection();
        }

        private async Task RefreshOwnershipFormsAsync()
        {
            List<OwnershipFormDto> ownershipForms = await WebClient.ExecuteApiRequestAsync(new QueryOwnershipForms());

            OwnershipForms = ownershipForms
                .OrderBy(x => x.Name)
                .Select(x => new ComboBoxItem(x.Id, x.Name))
                .ToReadOnlyObservableCollection();

            _owhershipFormNames = OwnershipForms
                .ToDictionary(x => x.Id, x => x.DisplayValue);
        }

        private async Task RefreshSupplierInvoiceProcessorsAsync()
        {
            List<SupplierInvoiceProcessorDto> processors = await WebClient.ExecuteApiRequestAsync(new QuerySupplierInvoiceProcessors());

            SupplierInvoiceProcessors = processors
                .OrderBy(x => x.Name)
                .ToReadOnlyObservableCollection();
        }

        private void HandlePreviewKeyDown(KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                OkCommand.Execute(null);
            }
            else if (e.Key == Key.Escape)
            {
                CancelCommand.Execute(null);
            }
        }

        private void HandleSelectionChanged(ValueChangedEventArgs<FrameworkElement> e)
        {
            switch (e.NewValue.Name)
            {
                case "ContactsTab":
                    IsHelpVisible = true;

                    if (Contacts == null)
                    {
                        RefreshContactsCommand.Execute(null);
                    }

                    break;
                case "TemplatesTab":
                    IsHelpVisible = true;
                    break;
                case "LogisticsTab":
                    IsHelpVisible = true;

                    if (SupplierWarehouses == null)
                    {
                        RefreshSupplierWarehousesCommand.Execute(null);
                    }

                    if (Carries == null)
                    {
                        RefreshSupplierCarriesCommand.Execute(null);
                    }

                    break;
                case "ParsersTab":
                    IsHelpVisible = true;

                    if (SupplierCategories == null)
                    {
                        RefreshSupplierCategoriesAbcCommand.Execute(null);
                    }

                    break;
                default:
                    IsHelpVisible = false;
                    break;
            }
        }

        private async Task RefreshSupplierCarriesAsync()
        {
            if (IsNew)
            {
                return;
            }

            IReadOnlyCollection<SupplierCarryDto> supplierCarries = await WebClient.ExecuteApiRequestAsync(new QueryContractorCarries(Model.Id));
            Carries = supplierCarries.Select(x => Mapper.Map(x, new SupplierCarryViewItem())).ToObservableCollection();
        }

        private async Task RefreshSupplierWarehousesAsync()
        {
            if (IsNew)
            {
                return;
            }

            IReadOnlyCollection<SupplierWarehouseDto> supplierWarehouses = await WebClient.ExecuteApiRequestAsync(new QueryContractorWarehouses(Model.Id));
            SupplierWarehouses = supplierWarehouses.Select(x => Mapper.Map<SupplierWarehouseViewItem>(x)).ToObservableCollection();
        }

        private async Task RefreshContactsAsync()
        {
            if (IsNew)
            {
                return;
            }

            IReadOnlyCollection<ContractorContactDto> contacts = await WebClient.ExecuteApiRequestAsync(new QueryContractorContacts(Model.Id));
            ObservableCollection<ContractorContactViewItem> items = contacts.Select(x => Mapper.Map(x, ContractorContactViewItem.Create())).ToObservableCollection();
            Contacts = items.Select(x => x.SetCityName(_allCities.ToReadOnlyObservableCollection())).ToObservableCollection();
        }

        private async Task EditContractorClassAsync()
        {
            ChooseAbcClassViewModel viewModel = DialogDocumentManagerService.ShowView<ChooseAbcClassViewModel>(Model.AbcType, this);

            if (!viewModel.IsOk)
            {
                return;
            }

            try
            {
                ContractorDto contractor = await WebClient.ExecuteApiRequestAsync(new UpdateContractorAbcType(Model.Id, viewModel.ResultType.Id));

                SetData(contractor);

                MessageFacadeService.ShowNotificationInfo($"Контрагенту №{contractor.Id} задан класс {Model.AbcType.Name}");
            }
            catch (UnexpectedErrorException exception)
            {
                Logger.LogError(exception, "Failed to update ABC category");
                ShowValidationResultView(Resources.ServerConnectError, new[] { new ValidationResultItem(Resources.ServerUnavailable, true) });
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Error while updating ABC category");
                MessageFacadeService.ShowNotificationError("Ошибка при обновлении категории поставщика");
            }
        }

        private async Task RefreshSupplierCategoriesAbcAsync()
        {
            if (IsNew)
            {
                return;
            }

            try
            {
                List<SupplierCategoryAbcDto> result = await WebClient.ExecuteApiRequestAsync(new QuerySupplierCategoriesAbc(Model.Id));
                IReadOnlyCollection<SupplierCategoryAbcDto> supplierCategoryAbcDtos = result;
                SetSupplierCategories(supplierCategoryAbcDtos);
            }
            catch (UnexpectedErrorException exception)
            {
                Logger.LogError(exception, "Failed to create SupierCategoryAbc");
                ShowValidationResultView(Resources.ServerConnectError, new[] { new ValidationResultItem(Resources.ServerUnavailable, true) });
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Error while refreshing SupplierCategoriesAbc");
                MessageFacadeService.ShowNotificationError("Ошибка при загрузке категорий поставщика");
            }
        }

        private async Task CreateSupplierCategoryAbcAsync()
        {
            ChooseCategoryViewModel viewModel = DialogDocumentManagerService.ShowView<ChooseCategoryViewModel>(new ChooseCategoryParameter(false, null), this);

            if (!viewModel.IsOk)
            {
                return;
            }

            try
            {
                SupplierCategoryAbcSaveDto createDto = new SupplierCategoryAbcSaveDto
                {
                    AbcId = Model.AbcType.Id,
                    ContractorId = Model.Id,
                    CategoryId = viewModel.SelectedCategory.Id
                };

                Result<SupplierCategoryAbcDto> result = await WebClient.ExecuteApiRequestAsync(new CreateSupplierCategoryAbc(Model.Id, createDto));

                SupplierCategoryAbcViewItem viewItem = Mapper.Map<SupplierCategoryAbcViewItem>(result.Data);

                SupplierCategories.Insert(0, viewItem);
                SetSupplierCategories(SupplierCategories);

                string abcName = Dictionaries.GetItemById<AbcType>(viewItem.AbcId).Name;

                MessageFacadeService.ShowNotificationInfo($"Поставщику присвоен класс {abcName} в категории {viewItem.Category?.Name}");
            }
            catch (UnexpectedSatusException exception)
            {
                MessageFacadeService.ShowNotificationError("Ошибка при создании категории поставщика");
                ShowValidationResultView("Ошибки при создании", exception.GetErrorItems());
            }
            catch (UnexpectedErrorException exception)
            {
                Logger.LogError(exception, "Failed to create SupierCategoryAbc");
                ShowValidationResultView(Resources.ServerConnectError, new[] { new ValidationResultItem(Resources.ServerUnavailable, true) });
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Error while creating SupplierCategoryAbc");
                MessageFacadeService.ShowNotificationError("Ошибка при создании категории поставщика");
            }
        }

        private void AddCurrencyPermission()
        {
            SelectItemViewModel viewModel = DialogDocumentManagerService.ShowView<SelectItemViewModel>(new SelectItemParameter(Currencies, "Выбор валюты", "Валюта"), this);

            if (!viewModel.IsOk)
            {
                return;
            }

            if (Model.CurrencyPermissions?.Any(x => x.CurrencyId == viewModel.SelectedItem!.Value.Id) == true)
            {
                MessageFacadeService.ShowNotificationWarning("Такая валюта уже добавлена");
                return;
            }

            Model.CurrencyPermissions ??= new ObservableCollection<ContractorCurrencyPermissionViewItem>();

            Model.CurrencyPermissions.Add(new ContractorCurrencyPermissionViewItem()
            {
                Id = 0,
                CurrencyId = viewModel.SelectedItem!.Value.Id,
                Sale = false,
                Purchase = false,
                CurrencyControl = false
            });
        }

        private void DeleteCurrencyPermission()
        {
            Model.CurrencyPermissions.Remove(SelectedCurrencyPermission);

            SelectedCurrencyPermission = null;
        }

        private async Task EditSupplierCategoryAbcAsync(SupplierCategoryAbcViewItem viewItem)
        {
            ChooseAbcClassViewModel viewModel = DialogDocumentManagerService.ShowView<ChooseAbcClassViewModel>(viewItem.AbcType, this);

            if (!viewModel.IsOk)
            {
                return;
            }

            try
            {
                SupplierCategoryAbcSaveDto saveDto = new SupplierCategoryAbcSaveDto
                {
                    Id = viewItem.Id,
                    AbcId = viewModel.ResultType.Id,
                    ContractorId = viewItem.ContractorId,
                    CategoryId = viewItem.CategoryId
                };

                Result<SupplierCategoryAbcDto> result = await WebClient.ExecuteApiRequestAsync(new UpdateSupplierCategoryAbc(saveDto));

                Mapper.Map(result.Data, viewItem);

                string abcName = Dictionaries.GetItemById<AbcType>(viewItem.AbcId).Name;

                MessageFacadeService.ShowNotificationInfo($"Поставщику присвоен класс {abcName} в категории {viewItem.Category.Name}");
            }
            catch (UnexpectedErrorException exception)
            {
                Logger.LogError(exception, "Failed to update SupierCategoryAbc");
                ShowValidationResultView(Resources.ServerConnectError, new[] { new ValidationResultItem(Resources.ServerUnavailable, true) });
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Error while updating SupplierCategoryAbc");
                MessageFacadeService.ShowNotificationError("Ошибка при обновлении категории поставщика");
            }
        }

        private async Task DeleteSupplierCategoryAbcAsync(SupplierCategoryAbcViewItem viewItem)
        {
            if (!MessageFacadeService.Confirm("Вы уверены?"))
            {
                return;
            }

            try
            {
                await WebClient.ExecuteApiRequestAsync(new DeleteSupplierCategoryAbc(Model.Id, viewItem.Id));
                SupplierCategories.RemoveAll(x => x.Id == viewItem.Id);
                SetSupplierCategories(SupplierCategories);

                MessageFacadeService.ShowNotificationInfo("Запись успешно удалена");
            }
            catch (UnexpectedErrorException exception)
            {
                Logger.LogError(exception, "Failed to delete SupierCategoryAbc");
                ShowValidationResultView(Resources.ServerConnectError, new[] { new ValidationResultItem(Resources.ServerUnavailable, true) });
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Error while deleting SupplierCategoryAbc");
                MessageFacadeService.ShowNotificationError("Ошибка при удалении категории поставщика");
            }
        }

        private void SetSupplierCategories(IEnumerable<SupplierCategoryAbcViewItem> supplierCategoryAbcDtos)
        {
            SupplierCategories = supplierCategoryAbcDtos
                .OrderBy(x => x.Category.Level)
                .ThenBy(x => x.Category.Position)
                .ToObservableCollection();
        }

        private void SetSupplierCategories(IReadOnlyCollection<SupplierCategoryAbcDto> supplierCategoryAbcDtos)
        {
            SetSupplierCategories(supplierCategoryAbcDtos.Select(x => Mapper.Map<SupplierCategoryAbcViewItem>(x)));
        }

        private void CreateOrganization()
        {
            GetTextFromUserParameter fromUserParameter = new GetTextFromUserParameter(
                "Укажите название",
                "Создание организации",
                null,
                "Не валидное значение. ");

            GetTextFromUserViewModel fromUserViewModel = DialogDocumentManagerService.ShowView<GetTextFromUserViewModel>(fromUserParameter, this);

            if (!fromUserViewModel.IsOk)
            {
                return;
            }

            if (fromUserViewModel.Content.Length > 90)
            {
                MessageFacadeService.ShowNotificationError("Максимально допустимая длина 90 символов");
                return;
            }

            string organization = fromUserViewModel.Content.Trim();

            Organizations.Add(organization);
            Organizations = Organizations.OrderBy(x => x).ToObservableCollection();
            Model.Organization = organization;
        }

        private void CreateForeignCity()
        {
            DialogDocumentManagerService.ShowView<ForeignCityCreateViewModel>(Model?.CountryId, this);
        }

        private void EditParserSettings()
        {
            SizeableDialogDocumentManagerService.ShowView<ParserSettingsViewModel>(new ParserSettingsParameter(SelectedParserSettings.Id), this);
        }

        private void AddParserSettings()
        {
            SizeableDialogDocumentManagerService.ShowView<ParserSettingsViewModel>(new ParserSettingsParameter(0, Model.Id), this);
        }

        private async Task SetAsMainCarryAsync(SupplierCarryViewItem carry)
        {
            if (!MessageFacadeService.Confirm("Вы уверены?"))
            {
                return;
            }

            try
            {
                await WebClient.ExecuteApiRequestAsync(new SetAsMainContractorCarry(Model.Id, carry.Id));

                MessageFacadeService.ShowNotificationInfo("Логистика успешно сделана основной");

                RefreshSupplierCarriesCommand.Execute(null);
            }
            catch (UnexpectedSatusException exception)
            {
                ShowValidationResultView("Ошибки", exception.GetErrorItems());
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Failed to set supplier carry as main");
                MessageFacadeService.ShowNotificationError("Ошибка при выполнении операции");
            }
        }

        private async Task SetAsNotMainCarryAsync(SupplierCarryViewItem carry)
        {
            if (!MessageFacadeService.Confirm("Вы уверены?"))
            {
                return;
            }

            try
            {
                await WebClient.ExecuteApiRequestAsync(new SetAsNotMainContractorCarry(Model.Id, carry.Id));

                MessageFacadeService.ShowNotificationInfo("Логистика успешно изменена");

                RefreshSupplierCarriesCommand.Execute(null);
            }
            catch (UnexpectedSatusException exception)
            {
                ShowValidationResultView("Ошибки", exception.GetErrorItems());
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Failed to set supplier carry as not main");
                MessageFacadeService.ShowNotificationError("Ошибка при выполнении операции");
            }
        }

        private void SelectedSupplierInvoiceProcessorChanged()
        {
            Model.PurchaseProcessorName = SelectedSupplierInvoiceProcessor?.Name;
            Model.PurchaseAutoReserve = Model.PurchaseAutoReserve && (SelectedSupplierInvoiceProcessor?.AllowReserve ?? false);
            Model.PurchaseAutoPurchase = Model.PurchaseAutoPurchase && (SelectedSupplierInvoiceProcessor?.AllowPurchase ?? false);
        }

        private void OnParserSettingsMessage(ParserSettingsMessage message)
        {
            switch (message.MessageType)
            {
                case MessageType.Added:
                {
                    Model.ParserSettings ??= new ObservableCollection<ParserSettingsViewItem>();

                    Model.ParserSettings.Add(Mapper.Map<ParserSettingsViewItem>(message.Entity));
                    break;
                }

                case MessageType.Changed:

                    Model.ParserSettings.DoActionWithItem(x => x.Id == message.Entity.Id, x => Mapper.Map(message.Entity, x));
                    break;
            }
        }

        private void SelectedCountryChanged()
        {
            int countryId = Model.CountryId ?? Constants.UkraineCountryId;

            Cities = _allCities.Where(x => x.CountryId == countryId).ToReadOnlyObservableCollection();
        }

        private void OnForeignCityMessage(ForeignCityMessage message)
        {
            switch (message.MessageType)
            {
                case MessageType.Added:
                    _allCities.Add(new ContractorCityItem { Id = message.Entity.Id, Name = message.Entity.Name, CountryId = message.Entity.CountryId });
                    SelectedCountryChanged();
                    break;
            }
        }

        private void SelectedCurrencyPermissionChanged()
        {
            if (Model is null || SelectedCurrencyPermission is null)
            {
                return;
            }

            Model.IsClient = Model.CurrencyPermissions.Any(x => x.Sale);

            Model.AutoSource = Model.CurrencyPermissions.Any(x => x.CurrencyId == Currency.UahId && x.Sale) && Model.AutoSource;

            Model.RaisePropertiesChanged();
        }

        private int GetLogisticsTabIndex()
        {
            int index = 1;

            if (Model.CanViewSettings)
            {
                index++;
            }

            if (Model.CanViewContacts)
            {
                index++;
            }

            if (Model.CanViewTemplates)
            {
                index++;
            }

            return index;
        }
    }
}