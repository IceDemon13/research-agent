using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using AutoMapper;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using DevExpress.Mvvm.UI;
using DevExpress.Xpf.Grid;
using DevExpress.XtraPrinting;
using Microsoft.Extensions.Logging;
using Telemart.Client.Common;
using Telemart.Client.Common.Messages;
using Telemart.Client.Common.MvvmEnhancements;
using Telemart.Client.Common.Services;
using Telemart.Client.Common.Utils;
using Telemart.Client.Data.Requests.Features.Contractor;
using Telemart.Client.Data.Requests.Features.ServiceProduct;
using Telemart.Client.Data.Requests.Features.Warehouse;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.Properties;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.Paging;
using Telemart.Client.TransferObjects.ServiceProduct;
using Telemart.Client.TransferObjects.Warehouse;
using Telemart.Client.ViewModels.Base;
using Telemart.Client.ViewModels.Nomenclature;

namespace Telemart.Client.ViewModels.Service.ServiceProducts
{
    public sealed class ServiceProductsViewModel : TelemartViewModelBase, ISupportHotkeys
    {
        public ServiceProductsViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            IMapper mapper,
            IMessenger messenger)
            : base(webClient, dictionaries, messageFacadeService)
        {
            Mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            Messenger = messenger ?? throw new ArgumentNullException(nameof(messenger));

            RefreshCommand = new AsyncCommand(RefreshAsync);
            EditCommand = new DelegateCommand(Edit, () => SelectedServiceProduct != null);
            CancelFilteringCommand = new DelegateCommand(CancelFiltering);
            SelectProductCommand = new DelegateCommand(SelectProduct);
            ExportCommand = new DelegateCommand<TableView>(Export, x => x != null);

            Filter = new ServiceProductFilterViewModel(webClient);

            ServiceProducts = new ObservableRangeCollection<ServiceProductViewItem>();

            Messenger.Register<ServiceProductMessage>(this, OnServiceProductMessage);
        }

        #region Command

        public IAsyncCommand RefreshCommand { get; }

        public IDelegateCommand EditCommand { get; }

        public IDelegateCommand CancelFilteringCommand { get; }

        public IDelegateCommand SelectProductCommand { get; }

        public IDelegateCommand ExportCommand { get; }

        #endregion

        #region INPC

        public ServiceProductFilterViewModel Filter { get; }

        public bool IsSearchPanelClosed
        {
            get { return GetProperty(() => IsSearchPanelClosed); }
            set { SetProperty(() => IsSearchPanelClosed, value); }
        }

        public ReadOnlyObservableCollection<ComboBoxItem> Warehouses
        {
            get { return GetProperty(() => Warehouses); }
            set { SetProperty(() => Warehouses, value); }
        }

        public ReadOnlyObservableCollection<ComboBoxItem> Contractors
        {
            get { return GetProperty(() => Contractors); }
            set { SetProperty(() => Contractors, value); }
        }

        public ReadOnlyObservableCollection<ServiceProductState> States
        {
            get { return GetProperty(() => States); }
            set { SetProperty(() => States, value); }
        }

        public ObservableRangeCollection<ServiceProductViewItem> ServiceProducts
        {
            get { return GetProperty(() => ServiceProducts); }
            set { SetProperty(() => ServiceProducts, value); }
        }

        public ServiceProductViewItem SelectedServiceProduct
        {
            get { return GetProperty(() => SelectedServiceProduct); }
            set { SetProperty(() => SelectedServiceProduct, value); }
        }

        public bool IsColumnChooserVisible
        {
            get { return GetProperty(() => IsColumnChooserVisible); }
            set { SetProperty(() => IsColumnChooserVisible, value); }
        }

        public decimal? TotalPrice => ServiceProducts?.Sum(x => x.PriceUsd);

        #endregion
        private IDocumentManagerService NonModalDialogDocumentManagerService => GetService<IDocumentManagerService>("NonModalDialogDocumentManagerService", ServiceSearchMode.PreferParents);

        private IDocumentManagerService SizeableDialogDocumentManagerService => GetService<IDocumentManagerService>("SizeableDialogDocumentManagerService", ServiceSearchMode.PreferParents);

        private ISaveFileDialogService SaveFileDialogService => GetService<ISaveFileDialogService>("ExcelSaveFileDialogService", ServiceSearchMode.PreferParents);

        private IMapper Mapper { get; }

        private IMessenger Messenger { get; }

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
                    case HotkeyMessageType.Edit:
                        EditCommand.Execute(null);
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

            States = Dictionaries.GetItems<ServiceProductState>().ToReadOnlyObservableCollection();

            CancelFilteringCommand.Execute(null);

            return Task.CompletedTask;
        }

        private async Task RefreshAsync()
        {
            try
            {
                ServiceProducts.Clear();

                await Filter.RefreshAsync();

                await Task.WhenAll(RefreshWarehousesAsync(), RefreshContractorsAsync());

                PagedResult<ServiceProductDto> serviceProducts = await WebClient.ExecuteApiRequestAsync(new QueryServiceProducts(Filter.GetFilteringItem()));

                ServiceProducts.AddRange(serviceProducts.Data.Select(Mapper.Map<ServiceProductViewItem>));

                RaisePropertyChanged(nameof(TotalPrice));
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, Resources.ErrorDuringDataLoading);
                MessageFacadeService.ShowNotificationError(Resources.ErrorDuringDataLoading);
            }

            async Task RefreshWarehousesAsync()
            {
                List<WarehouseDto> warehouses = await WebClient.ExecuteApiRequestAsync(new QueryWarehouses(), true).GetPagedResultDataAsync();

                Warehouses = warehouses.Select(x => new ComboBoxItem(x.Id, x.Name)).ToReadOnlyObservableCollection();
            }

            async Task RefreshContractorsAsync()
            {
                List<ContractorDto> contractors = await WebClient.ExecuteApiRequestAsync(new QueryContractors(), true).GetPagedResultDataAsync();

                Contractors = contractors.Select(x => new ComboBoxItem(x.Id, x.Name)).ToReadOnlyObservableCollection();
            }
        }

        private void Edit()
        {
            NonModalDialogDocumentManagerService.ShowEditorView<ServiceProductViewModel>(SelectedServiceProduct.Id, new ServiceProductParameter(SelectedServiceProduct.Id), this);
        }

        private void CancelFiltering()
        {
            Filter.ResetFilterValues();
            RefreshCommand.Execute(null);
        }

        private void OnServiceProductMessage(ServiceProductMessage message)
        {
            switch (message.MessageType)
            {
                case MessageType.Added:
                    ServiceProducts.Insert(0, Mapper.Map<ServiceProductViewItem>(message.Entity));
                    break;
                case MessageType.Changed:
                    ServiceProducts.DoActionWithItem(x => x.Id == message.Entity.Id, viewItem => Mapper.Map(message.Entity, viewItem));
                    break;
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

                Filter.Product = product.Name;
            }
        }

        private void Export(TableView tableView)
        {
            if (tableView?.Grid == null)
            {
                return;
            }

            string fileName = $"Service_Product_{DateTime.Now:yyyy-MM-dd}";
            string folderPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

            ((FileDialogServiceBase)SaveFileDialogService).InitialDirectory = folderPath;

            SaveFileDialogService.DefaultFileName = $"{fileName}";

            if (SaveFileDialogService.ShowDialog())
            {
                string filePath = SaveFileDialogService.File.GetFullName();

                try
                {
                    tableView.ExportToXlsx(filePath, new XlsxExportOptionsEx(TextExportMode.Value));

                    MessageFacadeService.ShowNotificationInfo("Данные успешно сохранены");
                }
                catch (IOException exception) when (exception.Message.Contains("being used by another process"))
                {
                    MessageFacadeService.ShowNotificationError($"Файл {fileName} занят другим процессом");
                }
                catch (Exception exception)
                {
                    Logger.LogError(exception, "Failed to export {FileName}", fileName);
                    MessageFacadeService.ShowNotificationError("Ошибка при экспорте");
                }
            }
        }
    }
}