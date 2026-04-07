using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using DevExpress.Mvvm.UI;
using DevExpress.Xpf.Grid;
using DevExpress.XtraPrinting;
using Microsoft.Extensions.Logging;
using Telemart.Client.Business;
using Telemart.Client.Common.Messages;
using Telemart.Client.Common.MvvmEnhancements;
using Telemart.Client.Common.Services;
using Telemart.Client.Common.Utils;
using Telemart.Client.Data.Requests.Features.Employee;
using Telemart.Client.Data.Requests.Features.PromoCode;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Data.WebClient.Security;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.Properties;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.PromoCode;
using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.PromoCode
{
    public class PromoCodesViewModel : TelemartViewModelBase, ISupportHotkeys
    {
        public PromoCodesViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            IMessenger messenger,
            IMapper mapper,
            ILockableOperationProcessorFactory lockableOperationProcessorFactory)
            : base(webClient, dictionaries, messageFacadeService)
        {
            Messenger = messenger ?? throw new ArgumentNullException(nameof(messenger));
            Mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            LockableOperationProcessorFactory = lockableOperationProcessorFactory ?? throw new ArgumentNullException(nameof(lockableOperationProcessorFactory));

            CreateCommand = new DelegateCommand(Create);
            EditCommand = new DelegateCommand(Edit, () => SelectedPromoCode != null);
            RefreshCommand = new AsyncCommand(RefreshAsync);
            SetShowOnSiteCommand = new AsyncCommand(SetShowOnSiteAsync, () => SelectedPromoCode != null && CanEdit);
            ExportCommand = new DelegateCommand<TableView>(Export, x => x != null);
            CancelFilteringCommand = new DelegateCommand(CancelFiltering);

            CanEdit = WebClient.IsOperationAllowed(BusinessOperation.PromoCodeUpdate);

            Messenger.Register<PromoCodeMessage>(this, OnPromoCodeMessage);

            PromoCodes = new ObservableRangeCollection<PromoCodeViewItem>();

            Filter = new PromoCodeFilterViewModel(webClient, dictionaries);
        }

        public PromoCodesViewModel()
        {
        }

        #region Commands

        public IDelegateCommand CancelFilteringCommand { get; }

        public IDelegateCommand ExportCommand { get; }

        public IDelegateCommand CreateCommand { get; }

        public IDelegateCommand EditCommand { get; }

        public IAsyncCommand SetShowOnSiteCommand { get; }

        public IAsyncCommand RefreshCommand { get; }

        #endregion

        #region INPC

        public ObservableRangeCollection<PromoCodeViewItem> PromoCodes
        {
            get { return GetProperty(() => PromoCodes); }
            set { SetProperty(() => PromoCodes, value); }
        }

        public PromoCodeViewItem SelectedPromoCode
        {
            get { return GetProperty(() => SelectedPromoCode); }
            set { SetProperty(() => SelectedPromoCode, value); }
        }

        public ReadOnlyObservableCollection<PromoCodeType> PromoCodeTypes
        {
            get { return GetProperty(() => PromoCodeTypes); }
            set { SetProperty(() => PromoCodeTypes, value); }
        }

        public ReadOnlyObservableCollection<ComboBoxItem> Employees
        {
            get { return GetProperty(() => Employees); }
            set { SetProperty(() => Employees, value); }
        }

        public bool IsColumnChooserVisible
        {
            get { return GetProperty(() => IsColumnChooserVisible); }
            set { SetProperty(() => IsColumnChooserVisible, value); }
        }

        public bool IsSearchPanelClosed
        {
            get { return GetProperty(() => IsSearchPanelClosed); }
            set { SetProperty(() => IsSearchPanelClosed, value); }
        }

        public PromoCodeFilterViewModel Filter { get; }

        public bool CanEdit { get; private set; }

        #endregion

        private IDocumentManagerService DialogDocumentManagerService => GetService<IDocumentManagerService>("DialogDocumentManagerService", ServiceSearchMode.PreferParents);

        private IDocumentManagerService SizeableDialogDocumentManagerService => GetService<IDocumentManagerService>("SizeableDialogDocumentManagerService", ServiceSearchMode.PreferParents);

        private ISaveFileDialogService SaveFileDialogService => GetService<ISaveFileDialogService>("ExcelSaveFileDialogService", ServiceSearchMode.PreferParents);

        private IMapper Mapper { get; }

        private IMessenger Messenger { get; }

        private ILockableOperationProcessorFactory LockableOperationProcessorFactory { get; }

        public bool HandleHotkey(HotkeyMessage msg)
        {
            bool handled = false;
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

                case HotkeyMessageType.Add:
                    CreateCommand.Execute(null);
                    handled = true;
                    break;

                case HotkeyMessageType.ShowColumnChooser:
                    IsColumnChooserVisible = !IsColumnChooserVisible;
                    handled = true;
                    break;
            }

            return handled;
        }

        protected override async Task HandleLoadedAsync()
        {
            PromoCodeTypes = Dictionaries.GetItems<PromoCodeType>().ToReadOnlyObservableCollection();

            await Filter.RefreshAsync();

            await RefreshAsync();
        }

        protected override void OnInitializeInDesignMode()
        {
            CanEdit = true;
        }

        private void Create()
        {
            SizeableDialogDocumentManagerService.ShowView<PromoCodeViewModel>(new PromoCodeParameter(0), this);
        }

        private void Edit()
        {
            if (SelectedPromoCode.TypeId == PromoCodeType.Bundle.Id && !WebClient.IsOperationAllowed(BusinessOperation.PromoCodeBundleUpdate))
            {
                MessageFacadeService.ShowNotificationError("У вас нет прав на редактирование бандла");
                return;
            }

            if (SelectedPromoCode.TypeId == PromoCodeType.Bundle.Id && !WebClient.IsOperationAllowed(BusinessOperation.PromoCodeUpdate))
            {
                MessageFacadeService.ShowNotificationError("У вас нет прав на редактирование акции");
                return;
            }

            SizeableDialogDocumentManagerService.ShowView<PromoCodeViewModel>(new PromoCodeParameter(SelectedPromoCode.Id), this);
        }

        private void CancelFiltering()
        {
            Filter.ResetFilterValues();
            RefreshCommand.Execute(null);
        }

        private Task SetShowOnSiteAsync()
        {
            return LockableOperationProcessorFactory.Create<PromoCodeFullDto>().DoActionAsync(SelectedPromoCode.Id, EditDeliveryDate);

            void EditDeliveryDate(PromoCodeDto promoCode)
            {
                DialogDocumentManagerService.ShowView<PromoCodeSetShowInSiteViewModel>(new PromoCodeSetShowInSiteParameter(promoCode.Id, promoCode.ShowInSite), this);
            }
        }

        private void Export(TableView tableView)
        {
            if (tableView?.Grid == null)
            {
                return;
            }

            string fileName = $"Promos_{DateTime.Now:yyyy-MM-dd}";
            string folderPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

            ((FileDialogServiceBase)SaveFileDialogService).InitialDirectory = folderPath;

            SaveFileDialogService.DefaultFileName = $"{fileName}";

            if (SaveFileDialogService.ShowDialog())
            {
                List<ColumnBase> columnChooserColumns = new List<ColumnBase>(tableView.ColumnChooserColumns);

                foreach (ColumnBase tableViewColumnChooserColumn in columnChooserColumns)
                {
                    tableViewColumnChooserColumn.Visible = true;
                }

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

                foreach (ColumnBase tableViewColumnChooserColumn in columnChooserColumns)
                {
                    tableViewColumnChooserColumn.Visible = false;
                }
            }
        }

        private async Task RefreshAsync()
        {
            try
            {
                List<PromoCodeDto> promoCodes = await WebClient.ExecuteApiRequestAsync(new QueryPromoCodes(Filter.GetFilteringItem()));
                List<EmployeeDto> employees = await WebClient.ExecuteApiRequestAsync(new QueryEmployees()).GetPagedResultDataAsync();

                PromoCodes.Clear();

                PromoCodes.AddRange(promoCodes
                    .OrderByDescending(x => x.DateStart)
                    .Select(x => Mapper.Map<PromoCodeViewItem>(x)));

                Employees = employees.Select(x => new ComboBoxItem(x.Id, x.Name)).ToReadOnlyObservableCollection();
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, Resources.ErrorDuringDataLoading);
                MessageFacadeService.ShowNotificationError(Resources.ErrorDuringDataLoading);
            }
        }

        private void OnPromoCodeMessage(PromoCodeMessage message)
        {
            PromoCodeDto entity = message.Entity;

            switch (message.MessageType)
            {
                case MessageType.Added:
                    PromoCodes.Insert(0, Mapper.Map<PromoCodeViewItem>(entity));
                    break;
                case MessageType.Changed:
                    PromoCodes.DoActionWithItem(x => x.Id == entity.Id, viewItem => Mapper.Map(entity, viewItem));
                    break;
            }
        }
    }
}
