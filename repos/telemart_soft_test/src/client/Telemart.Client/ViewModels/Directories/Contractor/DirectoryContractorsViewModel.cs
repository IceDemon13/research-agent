using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Navigation;
using AutoMapper;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using DevExpress.Xpf.Grid;
using Microsoft.Extensions.Logging;
using Telemart.Client.Business;
using Telemart.Client.Common;
using Telemart.Client.Common.ErrorHandler;
using Telemart.Client.Common.Messages;
using Telemart.Client.Common.MvvmEnhancements;
using Telemart.Client.Common.Services;
using Telemart.Client.Common.Utils;
using Telemart.Client.Core.Helpers;
using Telemart.Client.Data.Requests.Features.City;
using Telemart.Client.Data.Requests.Features.Contractor;
using Telemart.Client.Data.Requests.Features.Contractor.Actions;
using Telemart.Client.Data.Requests.Features.Employee;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Data.WebClient.Security;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.Properties;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.City;
using Telemart.Client.ViewModels.Directories.Contractor.ParserSettings;

namespace Telemart.Client.ViewModels.Directories.Contractor
{
    public sealed class DirectoryContractorsViewModel : ViewModelBase, ISupportHotkeys
    {
        private List<CustomComboBoxItem> cityFilterItems;
        private List<CustomComboBoxItem> responsibleFilterItems;
        private List<CustomComboBoxItem> subdivisionFilterItems;

        private IReadOnlyDictionary<int, ComboBoxItem> employeesDictionary;

        public DirectoryContractorsViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMapper mapper,
            IMessageFacadeService messageFacadeService,
            ILockableOperationProcessorFactory lockableOperationProcessorFactory,
            IErrorHandler errorHandler,
            IMessenger messenger,
            ILogger<DirectoryContractorsViewModel> logger)
            : this()
        {
            WebClient = webClient ?? throw new ArgumentNullException(nameof(webClient));
            Dictionaries = dictionaries ?? throw new ArgumentNullException(nameof(dictionaries));
            Mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            MessageFacadeService = messageFacadeService ?? throw new ArgumentNullException(nameof(messageFacadeService));
            Messenger = messenger ?? throw new ArgumentNullException(nameof(messenger));
            LockableOperationProcessorFactory = lockableOperationProcessorFactory ?? throw new ArgumentNullException(nameof(lockableOperationProcessorFactory));
            ErrorHandler = errorHandler ?? throw new ArgumentNullException(nameof(errorHandler));
            Logger = logger;

            Messenger.Register<ContractorMessage>(this, OnContractorMessage);
            Messenger.Register<ParserSettingsMessage>(this, OnParserSettingsMessage);

            CanCreate = WebClient.IsOperationAllowed(BusinessOperation.ContractorCreate);
            CanCreateInBuh1C = WebClient.IsOperationAllowed(BusinessOperation.ContractorCreateInBuh1C);

            ShowInactiveContractors = false;

            Contractors = new ObservableRangeCollection<ContractorViewItem>();

            ContractorsCollectionView = CollectionViewSource.GetDefaultView(Contractors);

            if (ContractorsCollectionView is ListCollectionView collection)
            {
                collection.CustomSort = Comparer<ContractorViewItem>.Create(
                    (x, y) =>
                    {
                        if (x.IsFolder == y.IsFolder)
                        {
                            return string.Compare(x.Name, y.Name, StringComparison.Ordinal);
                        }

                        return x.IsFolder ? -1 : 1;
                    });
            }
        }

        public DirectoryContractorsViewModel()
        {
            HandleLoadedCommand = new DelegateCommand(HandleLoaded);
            RefreshCommand = new AsyncCommand(RefreshAsync);
            AddFolderCommand = new DelegateCommand<ContractorViewItem>(AddFolder, x => x != null);
            AddContractorCommand = new DelegateCommand<ContractorViewItem>(AddContractor, x => x != null);
            EditContractorCommand = new AsyncCommand<object>(EditContractorAsync, CanEditContractor);
            UrlNavigateCommand = new DelegateCommand<RequestNavigateEventArgs>(UrlNavigate, CanUrlNavigate);
            ShowFilterPopupHandlerCommand = new DelegateCommand<FilterPopupEventArgs>(ShowFilterPopupHandler);
            CreateInBuh1CCommand = new AsyncCommand<ContractorViewItem>(CreateInBuh1CAsync, x => x?.IsFolder == false && x.Buh1CId == null);
        }

        #region Dependency properties

        public static string KeyFieldName => "Id";

        public static string ParentFieldName => "ParentId";

        public bool CanCreate
        {
            get { return GetProperty(() => CanCreate); }
            private set { SetProperty(() => CanCreate, value); }
        }

        public bool CanCreateInBuh1C
        {
            get { return GetProperty(() => CanCreateInBuh1C); }
            private set { SetProperty(() => CanCreateInBuh1C, value); }
        }

        public ObservableRangeCollection<ContractorViewItem> Contractors
        {
            get { return GetProperty(() => Contractors); }
            set { SetProperty(() => Contractors, value); }
        }

        public ICollectionView ContractorsCollectionView
        {
            get { return GetProperty(() => ContractorsCollectionView); }
            set { SetProperty(() => ContractorsCollectionView, value); }
        }

        public ReadOnlyObservableCollection<ContractorCityItem> Cities
        {
            get { return GetProperty(() => Cities); }
            private set { SetProperty(() => Cities, value); }
        }

        public bool IsColumnChooserVisible
        {
            get { return GetProperty(() => IsColumnChooserVisible); }
            set { SetProperty(() => IsColumnChooserVisible, value); }
        }

        public ContractorViewItem SelectedContractor
        {
            get { return GetProperty(() => SelectedContractor); }
            set { SetProperty(() => SelectedContractor, value); }
        }

        public ReadOnlyObservableCollection<Subdivision> Subdivisions
        {
            get { return GetProperty(() => Subdivisions); }
            private set { SetProperty(() => Subdivisions, value); }
        }

        public bool IsSearchControlFocused
        {
            get { return GetProperty(() => IsSearchControlFocused); }
            set { SetProperty(() => IsSearchControlFocused, value); }
        }

        public bool ShowInactiveContractors
        {
            get { return GetProperty(() => ShowInactiveContractors); }
            set => SetProperty(() => ShowInactiveContractors, value, () => RefreshCommand.Execute(null));
        }

        #endregion Dependency properties

        #region Commands

        public IDelegateCommand AddContractorCommand { get; }

        public IDelegateCommand AddFolderCommand { get; }

        public IAsyncCommand EditContractorCommand { get; }

        public IDelegateCommand HandleLoadedCommand { get; }

        public IAsyncCommand RefreshCommand { get; }

        public IDelegateCommand ShowFilterPopupHandlerCommand { get; }

        public IDelegateCommand UrlNavigateCommand { get; }

        public IAsyncCommand CreateInBuh1CCommand { get; }

        #endregion Commands

        private IDictionaries Dictionaries { get; }

        private ILogger<DirectoryContractorsViewModel> Logger { get; }

        private IMapper Mapper { get; }

        private IMessageFacadeService MessageFacadeService { get; }

        private IMessenger Messenger { get; }

        private IWebClient WebClient { get; }

        private IErrorHandler ErrorHandler { get; }

        private ILockableOperationProcessorFactory LockableOperationProcessorFactory { get; }

        public bool HandleHotkey(HotkeyMessage msg)
        {
            bool handled = false;

            if (msg.ModifierKeys == ModifierKeys.Alt)
            {
                switch (msg.Key)
                {
                    case Key.D:
                        ShowInactiveContractors = !ShowInactiveContractors;
                        handled = true;
                        break;
                }
            }
            else
            {
                switch (msg.HotkeyMessageType)
                {
                    case HotkeyMessageType.Refresh:
                        RefreshCommand.Execute(null);
                        handled = true;
                        break;
                    case HotkeyMessageType.Add:
                        if (CanCreate)
                        {
                            if ((msg.ModifierKeys & ModifierKeys.Shift) == ModifierKeys.Shift)
                            {
                                AddFolderCommand.Execute(SelectedContractor);
                            }
                            else
                            {
                                AddContractorCommand.Execute(SelectedContractor);
                            }
                        }

                        handled = true;
                        break;
                    case HotkeyMessageType.Edit:
                        EditContractorCommand.Execute(true);
                        handled = true;
                        break;
                    case HotkeyMessageType.ShowColumnChooser:
                        IsColumnChooserVisible = !IsColumnChooserVisible;
                        handled = true;
                        break;
                }
            }

            return handled;
        }

        private static List<CustomComboBoxItem> GetResponsibleFilterItems(IReadOnlyCollection<ContractorViewItem> contractors, IEnumerable<ComboBoxItem> employees)
        {
            HashSet<int> hashSet = new HashSet<int>(contractors.Where(x => x.EmployeeId.HasValue).Select(x => x.EmployeeId.Value).Distinct());

            return employees
                .Where(x => hashSet.Contains(x.Id))
                .Select(x => new CustomComboBoxItem { DisplayValue = x.DisplayValue, EditValue = x.DisplayValue })
                .OrderBy(x => x.DisplayValue)
                .ToList();
        }

        private static List<CustomComboBoxItem> GetCityFilterItems(IReadOnlyCollection<ContractorViewItem> contractors, ReadOnlyObservableCollection<ContractorCityItem> cities)
        {
            List<CustomComboBoxItem> items = new List<CustomComboBoxItem>
            {
                new CustomComboBoxItem { DisplayValue = Constants.UkraineDisplayValue, EditValue = Constants.UkraineDisplayValue }
            };

            HashSet<int> hashSet = new HashSet<int>(contractors.Where(x => x.CityId.HasValue).Select(x => x.CityId.Value));

            IOrderedEnumerable<CustomComboBoxItem> comboBoxItems = cities
                .Where(x => hashSet.Contains(x.Id))
                .Select(x => new CustomComboBoxItem { DisplayValue = x.Name, EditValue = x.Name })
                .OrderBy(x => x.DisplayValue);

            items.AddRange(comboBoxItems);

            return items;
        }

        private void AddContractor(ContractorViewItem viewItem)
        {
            AddContractorInternal(viewItem, false);
        }

        private void AddFolder(ContractorViewItem viewItem)
        {
            AddContractorInternal(viewItem, true);
        }

        private void AddContractorInternal(ContractorViewItem viewItem, bool isFolder)
        {
            ContractorViewItem parentFolder = GetParentFolder(viewItem);

            ContractorViewMessage message = new ContractorViewMessage(0)
            {
                IsFolder = isFolder
            };

            if (parentFolder != null)
            {
                message.ParentId = parentFolder.Id;
                message.CityId = parentFolder.CityId;
                message.Subdivision = parentFolder.Subdivision;
                message.EmployeeId = parentFolder.EmployeeId;
                message.IsCompetitor = parentFolder.IsCompetitor;
            }
            else
            {
                message.ParentId = 0;
            }

            Messenger.Send(message);
        }

        private async Task CreateInBuh1CAsync(ContractorViewItem contractor)
        {
            if (!MessageFacadeService.Confirm("Вы уверены"))
            {
                return;
            }

            LockableOperationProcessor<ContractorDto> lockableOperation = LockableOperationProcessorFactory.Create<ContractorDto>();

            await lockableOperation.DoOperationAsync(contractor.Id, async x =>
            {
                (await ErrorHandler.HandleErrorsAsync(ctx => WebClient.ExecuteApiRequestAsync(new CreateContractorInBuh1C(x.Id)), "создании контрагента в бухгалтерии", "Контрагент в бухгалтерии создан", this, true))
                .IfNotNull(result => Messenger.Send(new ContractorMessage(result.Data, MessageType.Changed)));
            });
        }

        private bool CanUrlNavigate(RequestNavigateEventArgs args)
        {
            return args.Uri.IsAbsoluteUri;
        }

        private Task EditContractorAsync(object parameter)
        {
            if (parameter is ContractorViewItem contractorParameter && !contractorParameter.IsFolder)
            {
                Messenger.Send(new ContractorViewMessage(contractorParameter.Id));
            }
            else if (SelectedContractor != null && parameter is bool b && b)
            {
                Messenger.Send(new ContractorViewMessage(SelectedContractor.Id));
            }

            return Task.CompletedTask;
        }

        private ContractorViewItem GetParentFolder(ContractorViewItem selectedContractorViewItem)
        {
            ContractorViewItem parentFolder = selectedContractorViewItem;

            while (parentFolder != null && !parentFolder.IsFolder)
            {
                parentFolder = Contractors.FirstOrDefault(x => x.Id == parentFolder.ParentId);
            }

            return parentFolder;
        }

        private void HandleLoaded()
        {
            if (Contractors == null || Contractors.Count == 0)
            {
                RefreshCommand.Execute(null);
            }
        }

        private void OnContractorMessage(ContractorMessage message)
        {
            switch (message.MessageType)
            {
                case MessageType.Added:
                    {
                        ShowInactiveContractors = true;

                        ContractorViewItem contractorViewItem = Map(message.Entity, new ContractorViewItem());

                        Contractors.Insert(0, contractorViewItem);
                        SelectedContractor = contractorViewItem;
                        break;
                    }

                case MessageType.Changed:
                    {
                        Contractors.DoActionWithItem(x => x.Id == message.Entity.Id, x => Map(message.Entity, x));
                        break;
                    }

                default:
                    {
                        Debug.WriteLine($"Unknown contractor message type {message.MessageType}");
                        break;
                    }
            }
        }

        private void OnParserSettingsMessage(ParserSettingsMessage message)
        {
            ContractorViewItem contractor = Contractors.First(x => x.Id == message.Entity.ContractorId);

            switch (message.MessageType)
            {
                case MessageType.Added:
                {
                    contractor.ParserSettings ??= new ObservableCollection<ParserSettingsViewItem>();

                    contractor.ParserSettings.Add(Mapper.Map<ParserSettingsViewItem>(message.Entity));
                    break;
                }

                case MessageType.Changed:

                    contractor.ParserSettings.DoActionWithItem(x => x.Id == message.Entity.Id, x => Mapper.Map(message.Entity, x));
                    break;
            }
        }

        private async Task RefreshAsync()
        {
            Contractors.Clear();

            try
            {
                Subdivisions = Dictionaries.GetItems<Subdivision>().ToReadOnlyObservableCollection();

                await Task.WhenAll(RefreshEmployeesAsync(), RefreshCitiesAsync());

                await RefreshContractorsAsync();

                cityFilterItems = GetCityFilterItems(Contractors, Cities);
                responsibleFilterItems = GetResponsibleFilterItems(Contractors, employeesDictionary.Values);
                subdivisionFilterItems = Subdivisions
                    .Select(x => new CustomComboBoxItem { DisplayValue = x.Name, EditValue = x })
                    .ToList();
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Error while loading contractors");
                MessageFacadeService.ShowNotificationError(Resources.ErrorDuringDataLoading);
            }
        }

        private async Task RefreshContractorsAsync()
        {
            List<ContractorDto> contractors = await WebClient.ExecuteApiRequestAsync(new QueryContractors(ShowInactiveContractors ? null : true)).GetPagedResultDataAsync();

            Contractors.Clear();
            Contractors.AddRange(contractors.Select(x => Map(x, new ContractorViewItem())));
        }

        private async Task RefreshEmployeesAsync()
        {
            List<EmployeeDto> employees = await WebClient.ExecuteApiRequestAsync(new QueryEmployees(), true).GetPagedResultDataAsync();

            employeesDictionary = employees.ToDictionary(x => x.Id, y => new ComboBoxItem(y.Id, y.Name, y.Active));
        }

        private async Task RefreshCitiesAsync()
        {
            List<CityDto> cities = await WebClient.ExecuteApiRequestAsync(new QueryCities(), true).GetPagedResultDataAsync();
            List<CityDto> citiesList = new List<CityDto>(cities) { new CityDto { Id = -1, Name = Constants.UkraineDisplayValue, Position = -1 } };

            List<ContractorCityItem> allCityItems = citiesList
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

            allCityItems.AddRange(foreignCitiesDto.Select(x => new ContractorCityItem { Id = x.Id, Name = x.Name, CountryId = x.CountryId }));

            Cities = allCityItems.ToReadOnlyObservableCollection();
        }

        private void ShowFilterPopupHandler(FilterPopupEventArgs e)
        {
            switch (e.Column.FieldName)
            {
                case nameof(ContractorViewItem.Subdivision):
                    e.ComboBoxEdit.ItemsSource = subdivisionFilterItems;
                    break;
                case nameof(ContractorViewItem.CityId):
                    e.ComboBoxEdit.ItemsSource = cityFilterItems;
                    break;
                case nameof(ContractorViewItem.EmployeeId):
                    e.ComboBoxEdit.ItemsSource = responsibleFilterItems;
                    break;
            }
        }

        private void UrlNavigate(RequestNavigateEventArgs args)
        {
            try
            {
                ProcessHelper.Start(args.Uri.AbsoluteUri);
            }
            catch (Exception exception)
            {
                Logger.LogWarning(exception, "Can't navigate uri");
            }
        }

        private bool CanEditContractor(object parameter)
        {
            return parameter is ContractorViewItem || (parameter is bool && (bool)parameter);
        }

        private ContractorViewItem Map(ContractorDto dto, ContractorViewItem viewItem)
        {
            ContractorViewItem mappedItem = Mapper.Map(dto, viewItem);

            mappedItem.Employee = employeesDictionary.GetValueOrDefault(dto.EmployeeId);

            return mappedItem;
        }
    }
}