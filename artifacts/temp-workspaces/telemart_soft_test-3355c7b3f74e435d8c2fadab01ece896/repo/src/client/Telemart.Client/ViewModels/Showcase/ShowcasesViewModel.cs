using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using AutoMapper;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using Microsoft.Extensions.Logging;
using Telemart.Client.Common;
using Telemart.Client.Common.ErrorHandler;
using Telemart.Client.Common.Messages;
using Telemart.Client.Common.MvvmEnhancements;
using Telemart.Client.Common.Services;
using Telemart.Client.Common.Utils;
using Telemart.Client.Data.Requests.Features.Category;
using Telemart.Client.Data.Requests.Features.Employee;
using Telemart.Client.Data.Requests.Features.Showcase;
using Telemart.Client.Data.Requests.Features.Warehouse;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Data.WebClient.Security;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.Properties;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.Debezium;
using Telemart.Client.TransferObjects.Showcase;
using Telemart.Client.TransferObjects.Warehouse;
using Telemart.Client.ViewModels.Base;
using Telemart.Client.ViewModels.Nomenclature;
using Telemart.Client.ViewModels.Store.Order;
using Telemart.Client.ViewModels.Store.Order.ProductInformation;

namespace Telemart.Client.ViewModels.Showcase
{
    public class ShowcasesViewModel : TelemartViewModelBase, ISupportHotkeys
    {
        public ShowcasesViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            IMapper mapper,
            ProductInformationViewModel productInformationViewModel,
            IErrorHandler errorHandler)
            : base(webClient, dictionaries, messageFacadeService)
        {
            ErrorHandler = errorHandler;

            CanCreate = WebClient.IsOperationAllowed(BusinessOperation.ShowcaseCreate);
            CanDelete = WebClient.IsOperationAllowed(BusinessOperation.ShowcaseDelete);

            CancelFilteringCommand = new DelegateCommand(CancelFiltering);
            RefreshCommand = new AsyncCommand(RefreshAsync);
            SelectProductCommand = new DelegateCommand(SelectProduct);
            ShowcaseCategoryCommand = new DelegateCommand(ShowcaseCategory);
            EditCommand = new AsyncCommand(EditAsync, () => SelectedShowcasesHistory != null);

            Filter = new ShowcaseFilterViewModel(webClient, mapper);

            ShowcasesHistories = new ObservableRangeCollection<AutoShowcasesHistoryViewItem>();
            ProductInformation = productInformationViewModel;
        }

        public ShowcasesViewModel()
        {
        }

        #region Commands

        public IDelegateCommand CancelFilteringCommand { get; }

        public IDelegateCommand SelectProductCommand { get; }

        public IDelegateCommand ShowcaseCategoryCommand { get; }

        public IAsyncCommand RefreshCommand { get; }

        public IAsyncCommand EditCommand { get; }

        #endregion

        #region INPC

        public bool CanCreate
        {
            get { return GetProperty(() => CanCreate); }
            private set { SetProperty(() => CanCreate, value); }
        }

        public bool CanDelete
        {
            get { return GetProperty(() => CanDelete); }
            private set { SetProperty(() => CanDelete, value); }
        }

        public ShowcaseFilterViewModel Filter { get; }

        public bool IsSearchPanelClosed
        {
            get { return GetProperty(() => IsSearchPanelClosed); }
            set { SetProperty(() => IsSearchPanelClosed, value); }
        }

        public ReadOnlyObservableCollection<ComboBoxItem> Warehouses
        {
            get { return GetProperty(() => Warehouses); }
            private set { SetProperty(() => Warehouses, value); }
        }

        public ReadOnlyObservableCollection<ComboBoxItem> Categories
        {
            get { return GetProperty(() => Categories); }
            private set { SetProperty(() => Categories, value); }
        }

        public ObservableCollection<AutoShowcasesHistoryViewItem> ShowcasesHistories
        {
            get { return GetProperty(() => ShowcasesHistories); }
            private set { SetProperty(() => ShowcasesHistories, value); }
        }

        public AutoShowcasesHistoryViewItem SelectedShowcasesHistory
        {
            get { return GetProperty(() => SelectedShowcasesHistory); }
            set { SetProperty(() => SelectedShowcasesHistory, value, SelectedShowcaseChanged); }
        }

        public bool IsColumnChooserVisible
        {
            get { return GetProperty(() => IsColumnChooserVisible); }
            set { SetProperty(() => IsColumnChooserVisible, value); }
        }

        public ProductInformationViewModel ProductInformation
        {
            get { return GetProperty(() => ProductInformation); }
            private set { SetProperty(() => ProductInformation, value); }
        }

        public ReadOnlyObservableCollection<ComboBoxItem> AllEmployees
        {
            get { return GetProperty(() => AllEmployees); }
            private set { SetProperty(() => AllEmployees, value); }
        }

        #endregion

        private IDocumentManagerService DialogDocumentManagerService => GetService<IDocumentManagerService>("DialogDocumentManagerService", ServiceSearchMode.PreferParents);

        private IDocumentManagerService SizeableDialogDocumentManagerService => GetService<IDocumentManagerService>("SizeableDialogDocumentManagerService", ServiceSearchMode.PreferParents);

        private IErrorHandler ErrorHandler { get; }

        public bool HandleHotkey(HotkeyMessage msg)
        {
            bool handled = false;

            if (msg.ModifierKeys == ModifierKeys.Alt)
            {
                switch (msg.Key)
                {
                    case Key.L:
                        IsSearchPanelClosed = !IsSearchPanelClosed;
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
                    case HotkeyMessageType.ShowColumnChooser:
                        IsColumnChooserVisible = !IsColumnChooserVisible;
                        handled = true;
                        break;
                }
            }

            return handled;
        }

        protected override Task HandleLoadedAsync()
        {
            IsSearchPanelClosed = false;

            CancelFilteringCommand.Execute(null);

            return Task.CompletedTask;
        }

        protected override void OnInitializeInDesignMode()
        {
            base.OnInitializeInDesignMode();

            CanCreate = true;
            CanDelete = true;
        }

        private static AutoShowcasesHistoryViewItem MapToViewItem(ShowcaseHistoryDto history)
        {
            return new AutoShowcasesHistoryViewItem()
            {
                ShowcaseHistoryId = history.ShowcaseHistoryId,
                ShowcaseId = history.ShowcaseId,
                WarehouseId = history.WarehouseId,
                ProductId = history.ProductId,
                Name = history.ProductFullNameRu,
                NameUkr = history.ProductFullNameUkr,
                NameEn = history.ProductFullNameEn,
                CategoryId = history.CategoryId,
                CapacityNew = history.CapacityNew,
                CapacityOld = history.CapacityOld,
                ActiveOld = history.ActiveOld,
                ActiveNew = history.ActiveNew,
                ModifiedOn = history.ModifiedOn,
                ModifiedBy = history.ModifiedBy
            };
        }

        private void CancelFiltering()
        {
            Filter.ResetFilterValues();
            RefreshCommand.Execute(null);
        }

        private async Task RefreshAsync()
        {
            try
            {
                await Task.WhenAll(RefreshWarehouses(), RefreshCategories(), RefreshEmployeesAsync());

                await Filter.RefreshAsync();

                await RefreshHistoriesByFilterAsync();
            }
            catch (Exception exception)
            {
                MessageFacadeService.ShowNotificationError(Resources.ErrorDuringDataLoading);
                Logger.LogError(exception, Resources.ErrorDuringDataLoading);
            }

            async Task RefreshWarehouses()
            {
                List<WarehouseDto> warehouses = await WebClient.ExecuteApiRequestAsync(new QueryWarehouses(), true).GetPagedResultDataAsync();

                Warehouses = warehouses
                    .Select(x => new ComboBoxItem(x.Id, x.Name))
                    .ToReadOnlyObservableCollection();
            }

            async Task RefreshCategories()
            {
                List<CategoryDto> categories = await WebClient.ExecuteApiRequestAsync(new QueryCategories(), true).GetPagedResultDataAsync();

                Categories = categories
                    .Select(x => new ComboBoxItem(x.Id, x.Name))
                    .ToReadOnlyObservableCollection();
            }

            async Task RefreshEmployeesAsync()
            {
                List<EmployeeDto> employees = await WebClient.ExecuteApiRequestAsync(new QueryEmployees(), true).GetPagedResultDataAsync();

                AllEmployees = employees
                    .Select(x => new ComboBoxItem(x.Id, x.Name, x.Active))
                    .ToReadOnlyObservableCollection();
            }
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

                Filter.Product = new ComboBoxItem(product.Id, product.Name);
            }
        }

        private void ShowcaseCategory()
        {
            if (Filter.GetFilteringItem() is ShowcaseFilteringItem filteringItem && filteringItem.From.HasValue && filteringItem.To.HasValue)
            {
                SizeableDialogDocumentManagerService.ShowView<ShowcaseCategoryHistoryViewModel>(new ShowcaseCategoryHistoryParameter(filteringItem.From, filteringItem.To), this);
                return;
            }

            MessageFacadeService.ShowNotificationError("Укажите период в поиске");
        }

        private void SelectedShowcaseChanged()
        {
            ProductInformation.ClearProduct();

            if (SelectedShowcasesHistory != null)
            {
                ProductInformation.ProductId = new ProductInfoId(SelectedShowcasesHistory.ProductId, Currency.UahId);
            }
        }

        private async Task RefreshHistoriesByFilterAsync()
        {
            ShowcasesHistories.Clear();

            List<ShowcaseHistoryDto> histories = await ErrorHandler.HandleErrorsAsync(
                _ => WebClient.ExecuteApiRequestAsync(new QueryShowcaseHistoriesByFilter(Filter.GetFilteringItem())),
                "при загрузке истории",
                null,
                this,
                true,
                false);

            if (histories?.Any() == true)
            {
                ShowcasesHistories.AddRange(histories.Select(MapToViewItem));
            }
        }

        private async Task EditAsync()
        {
            ShowcaseDto isDeleted = await ErrorHandler.HandleErrorsAsync(
                _ => WebClient.ExecuteApiRequestAsync(new QueryShowcase(SelectedShowcasesHistory.ShowcaseId)),
                null,
                null,
                this,
                false,
                false,
                false);

            if (isDeleted != null)
            {
                DialogDocumentManagerService.ShowView<ShowcaseViewModel>(new ShowcaseParameter(SelectedShowcasesHistory.ShowcaseId), this);
            }
        }
    }
}