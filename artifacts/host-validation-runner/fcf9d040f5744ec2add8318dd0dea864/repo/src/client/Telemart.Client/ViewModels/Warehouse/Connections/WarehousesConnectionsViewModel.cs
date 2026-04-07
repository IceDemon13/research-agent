using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using DevExpress.Mvvm;
using DevExpress.Mvvm.DataAnnotations;
using DevExpress.Mvvm.Native;
using Microsoft.Extensions.Logging;
using Telemart.Client.Common.Messages;
using Telemart.Client.Common.MvvmEnhancements;
using Telemart.Client.Common.Services;
using Telemart.Client.Data.Requests.Features.Warehouse;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.Properties;
using Telemart.Client.TransferObjects.Warehouse;
using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.Warehouse.Connections
{
    public class WarehousesConnectionsViewModel : TelemartViewModelBase, ISupportHotkeys, IDataErrorInfo
    {
        public WarehousesConnectionsViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService)
            : base(webClient, dictionaries, messageFacadeService)
        {
            CancelFilteringCommand = new DelegateCommand(CancelFiltering);
            RefreshCommand = new AsyncCommand(RefreshAsync, () => WarehouseFromId.HasValue && WarehouseToId.HasValue && DateStart.HasValue);
            EditRoutesCommand = new DelegateCommand<WarehousesConnectionsViewItem>(EditRoutes, x => x.RouteId > 0);
        }

        public WarehousesConnectionsViewModel()
        {
        }

        #region Commands

        public IDelegateCommand CancelFilteringCommand { get; }

        public IDelegateCommand EditRoutesCommand { get; }

        public IAsyncCommand RefreshCommand { get; }

        #endregion

        #region Collections

        public ReadOnlyObservableCollection<ComboBoxItem> Warehouses
        {
            get { return GetProperty(() => Warehouses); }
            set { SetProperty(() => Warehouses, value); }
        }

        public ReadOnlyObservableCollection<WarehousesConnectionsViewItem> Connections
        {
            get { return GetProperty(() => Connections); }
            set { SetProperty(() => Connections, value); }
        }

        #endregion

        #region INPC

        public DateTime? DateStart
        {
            get { return GetProperty(() => DateStart); }
            set { SetProperty(() => DateStart, value); }
        }

        public int? WarehouseFromId
        {
            get { return GetProperty(() => WarehouseFromId); }
            set { SetProperty(() => WarehouseFromId, value); }
        }

        public int? WarehouseToId
        {
            get { return GetProperty(() => WarehouseToId); }
            set { SetProperty(() => WarehouseToId, value); }
        }

        public int MaxTransitCount
        {
            get { return GetProperty(() => MaxTransitCount); }
            set { SetProperty(() => MaxTransitCount, value); }
        }

        public bool IsSearchPanelClosed
        {
            get { return GetProperty(() => IsSearchPanelClosed); }
            set { SetProperty(() => IsSearchPanelClosed, value); }
        }

        #endregion

        string IDataErrorInfo.Error => string.Empty;

        string IDataErrorInfo.this[string columnName] => IDataErrorInfoHelper.GetErrorText(this, columnName);

        private IDocumentManagerService DialogDocumentManagerService => GetService<IDocumentManagerService>("DialogDocumentManagerService", ServiceSearchMode.PreferParents);

        public static void BuildMetadata(MetadataBuilder<WarehousesConnectionsViewModel> builder)
        {
            builder.Property(x => x.DateStart).Required(() => Resources.RequiredErrorMessage);
            builder.Property(x => x.WarehouseFromId).Required(() => Resources.RequiredErrorMessage);
            builder.Property(x => x.WarehouseToId).Required(() => Resources.RequiredErrorMessage);
        }

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
                }
            }

            return handled;
        }

        protected override async Task HandleLoadedAsync()
        {
            if (Connections != null)
            {
                return;
            }

            IsSearchPanelClosed = true;

            await RefreshWarehousesAsync();

            CancelFiltering();
        }

        protected override void OnInitializeInDesignMode()
        {
            Connections = Array.Empty<WarehousesConnectionsViewItem>().ToReadOnlyObservableCollection();
        }

        private static IEnumerable<WarehousesConnectionsViewItem> MapConnections(IReadOnlyCollection<IReadOnlyCollection<WarehouseConnectionDto>> connections)
        {
            foreach (IReadOnlyCollection<WarehouseConnectionDto> connection in connections)
            {
                WarehouseConnectionDto[] singlePathConnections = connection
                    .OrderBy(x => x.StartDate)
                    .ToArray();

                WarehouseConnectionDto firstConnection = singlePathConnections.First();
                WarehouseConnectionDto lastConnection = singlePathConnections.Last();

                WarehousesConnectionsViewItem item = new WarehousesConnectionsViewItem
                {
                    StartDate = firstConnection.StartDate,
                    FromWarehouseId = firstConnection.FromWarehouseId,

                    EndDate = lastConnection.EndDate,
                    ToWarehouseId = lastConnection.ToWarehouseId,

                    Weight = singlePathConnections.Sum(x => x.Weight),

                    Connections = singlePathConnections
                        .Select(x => new WarehousesConnectionsViewItem
                        {
                            StartDate = x.StartDate,
                            EndDate = x.EndDate,
                            FromWarehouseId = x.FromWarehouseId,
                            ToWarehouseId = x.ToWarehouseId,
                            RouteId = x.RouteId,
                            RouteTimeId = x.RouteTimeId,
                            Weight = x.Weight
                        }).ToReadOnlyObservableCollection()
                };

                yield return item;
            }
        }

        private void EditRoutes(WarehousesConnectionsViewItem item)
        {
            if (item.RouteTimeId > 0)
            {
                DialogDocumentManagerService.ShowView<WarehouseRouteEditViewModel>(new WarehouseRouteEditParameter(item.FromWarehouseId, item.RouteId, item.RouteTimeId), this);
            }
        }

        private void CancelFiltering()
        {
            ResetFilterValues();
        }

        private async Task RefreshAsync()
        {
            if (IDataErrorInfoHelper.HasErrors(this))
            {
                return;
            }

            try
            {
                WarehousesConnectionsResult result = await WebClient.ExecuteApiRequestAsync(new QueryWarehousesConnections(DateStart.Value, WarehouseFromId.Value, WarehouseToId.Value, MaxTransitCount));

                Connections = MapConnections(result.Connections).ToReadOnlyObservableCollection();
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, Resources.ErrorDuringDataLoading);
                MessageFacadeService.ShowNotificationError(Resources.ErrorDuringDataLoading);
            }
        }

        private async Task RefreshWarehousesAsync()
        {
            List<WarehouseDto> warehouses = await WebClient.ExecuteApiRequestAsync(new QueryWarehouses(), true).GetPagedResultDataAsync();

            Warehouses = warehouses
                .Where(x => x.Active == 1)
                .OrderByDescending(x => x.Position)
                .Select(x => new ComboBoxItem(x.Id, x.Name))
                .ToReadOnlyObservableCollection();
        }

        private void ResetFilterValues()
        {
            WarehouseFromId = null;
            WarehouseToId = null;
            DateStart = null;
            MaxTransitCount = 3;
        }
    }
}