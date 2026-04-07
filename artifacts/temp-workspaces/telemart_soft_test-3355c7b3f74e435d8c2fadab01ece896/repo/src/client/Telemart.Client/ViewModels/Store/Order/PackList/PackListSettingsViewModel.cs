using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using DevExpress.Mvvm.DataAnnotations;
using DevExpress.Mvvm.Native;
using Telemart.Client.Common.ErrorHandler;
using Telemart.Client.Common.MvvmEnhancements;
using Telemart.Client.Common.Services;
using Telemart.Client.Data.Requests.Features.PackList;
using Telemart.Client.Data.Requests.Features.Warehouse;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.TransferObjects.PackList;
using Telemart.Client.TransferObjects.Paging;
using Telemart.Client.TransferObjects.Warehouse;
using Telemart.Client.ViewModels.Base;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.ViewModels.Store.Order.PackList
{
    public sealed class PackListSettingsViewModel : TelemartDialogViewModelBase
    {
        private readonly IErrorHandler _errorHandler;

        private ReadOnlyObservableCollection<WarehousePackListSettingDto> _warehousePackListSettingDtos;

        public PackListSettingsViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            IErrorHandler errorHandler)
            : base(webClient, dictionaries, messageFacadeService)
        {
            _errorHandler = errorHandler;
        }

        public PackListSettingsViewModel()
        {
        }

        public ReadOnlyObservableCollection<ComboBoxItem> Warehouses
        {
            get { return GetProperty(() => Warehouses); }
            set { SetProperty(() => Warehouses, value); }
        }

        public int? WarehouseId
        {
            get { return GetProperty(() => WarehouseId); }
            set { SetProperty(() => WarehouseId, value, WarehouseChanged); }
        }

        public int? OrderProductMaxLines
        {
            get { return GetProperty(() => OrderProductMaxLines); }
            set { SetProperty(() => OrderProductMaxLines, value); }
        }

        public decimal? MaxTotalWeight
        {
            get { return GetProperty(() => MaxTotalWeight); }
            set { SetProperty(() => MaxTotalWeight, value); }
        }

        public int? MaxSkuQuantity
        {
            get { return GetProperty(() => MaxSkuQuantity); }
            set { SetProperty(() => MaxSkuQuantity, value); }
        }

        public static void BuildMetadata(MetadataBuilder<PackListSettingsViewModel> builder)
        {
            builder.Property(x => x.WarehouseId)
                .MatchesRule(x => x != null, () => "Выберите склад");

            builder.Property(x => x.MaxTotalWeight)
                .MatchesInstanceRule((x, y) => y.WarehouseId == null || x > 0, () => "Максимальный вес заказа должен быть больше 0");

            builder.Property(x => x.MaxSkuQuantity)
                .MatchesInstanceRule((x, y) => y.WarehouseId == null || x > 0, () => "Максимальное количество одного sku  товара в заказе должно быть больше 0");

            builder.Property(x => x.OrderProductMaxLines)
                .MatchesInstanceRule((x, y) => y.WarehouseId == null || x == null || x > 0, () => "Максимальное количество строк в заказе должно быть больше 0");
        }

        protected override async Task HandleLoadedAsync()
        {
            PagedResult<WarehouseDto> warehouses = await WebClient.ExecuteApiRequestAsync(new QueryWarehouses(), true);

            Warehouses = warehouses.Data
                .Where(x => x.Active == 1)
                .Select(x => new ComboBoxItem(x.Id, x.Name))
                .ToReadOnlyObservableCollection();

            await LoadWarehousePackListSettingsAsync(Warehouses.Select(x => x.Id).ToArray());

            Title = "Настройки листа на сборку";
        }

        protected override async Task HandleOkAsync()
        {
            WarehousePackListSettingDto packListSetting = _warehousePackListSettingDtos?.FirstOrDefault(x => x.WarehouseId == WarehouseId);

            SaveWarehousePackListSettingDto saveDto = new SaveWarehousePackListSettingDto(WarehouseId.Value, OrderProductMaxLines, MaxTotalWeight.Value, MaxSkuQuantity.Value);

            Result<WarehousePackListSettingDto> result;

            if (packListSetting == null)
            {
                 result = await _errorHandler.HandleErrorsAsync(
                    _ => WebClient.ExecuteApiRequestAsync(new CreatePackListSetting(saveDto)),
                    "создании настроек листа на сборку",
                    "Создание настроек листа на сборку",
                    this,
                    true);
            }
            else
            {
                result = await _errorHandler.HandleErrorsAsync(
                    _ => WebClient.ExecuteApiRequestAsync(new UpdatePackListSetting(packListSetting.Id, saveDto)),
                    "обновлении настроек листа на сборку",
                    "Обновление настроек листа на сборку",
                    this,
                    true);
            }

            if (result.IsSuccess)
            {
                CloseOk();
            }
        }

        private async Task LoadWarehousePackListSettingsAsync(int[] warehouseIds)
        {
            WarehousePackListSettingsFilter filter = new WarehousePackListSettingsFilter(warehouseIds);

            List<WarehousePackListSettingDto> warehousePackListSettingDtos = await _errorHandler.HandleErrorsAsync(
                _ => WebClient.ExecuteApiRequestAsync(new QueryPackListSettings(filter)),
                "получении настроек листа",
                null,
                this,
                true,
                showNotification: false);

            _warehousePackListSettingDtos = warehousePackListSettingDtos?.Any() == true
                ? warehousePackListSettingDtos.ToReadOnlyObservableCollection()
                : Array.Empty<WarehousePackListSettingDto>().ToReadOnlyObservableCollection();
        }

        private void WarehouseChanged()
        {
            if (WarehouseId.HasValue)
            {
                WarehousePackListSettingDto packListSetting = _warehousePackListSettingDtos?.FirstOrDefault(x => x.WarehouseId == WarehouseId);

                MaxTotalWeight = packListSetting?.MaxTotalWeight ?? 40;
                MaxSkuQuantity = packListSetting?.MaxSkuQuantity ?? 25;
                OrderProductMaxLines = packListSetting?.OrderProductMaxLines;
            }
        }
    }
}