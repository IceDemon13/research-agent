using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using DynamicData;
using Telemart.Client.Common.ErrorHandler;
using Telemart.Client.Common.Services;
using Telemart.Client.Data.Requests.Features.AutoSource;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.TransferObjects.AutoSource;
using Telemart.Client.ViewModels.Base;
using Telemart.Client.ViewModels.Common;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.ViewModels.Store.Order.AutoSource
{
    public sealed class AutoSourceSettingsViewModel : TelemartDialogViewModelBase
    {
        private ReadOnlyObservableCollection<AutoSourceSettingDto> _autoSourceSettingDtos;
        private ReadOnlyObservableCollection<OrderProductSourceSettingDto> _orderProductSourceSettingDtos;

        public AutoSourceSettingsViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            IErrorHandler errorHandler,
            IMapper mapper)
            : base(webClient, dictionaries, messageFacadeService)
        {
            ErrorHandler = errorHandler;
            Mapper = mapper;

            AddOrderProductSourceSettingCommand = new DelegateCommand(AddOrderProductSourceSetting);
            EditOrderProductSourceSettingCommand = new DelegateCommand<OrderProductSourceSettingItem>(EditOrderProductSourceSetting, x => x != null);
            RemoveOrderProductSourceSettingCommand = new DelegateCommand<OrderProductSourceSettingItem>(RemoveOrderProductSourceSetting, x => x != null);
        }

        #region INPC

        public AutoSourceSettingItem CurrentWeightKoef
        {
            get { return GetProperty(() => CurrentWeightKoef); }
            set { SetProperty(() => CurrentWeightKoef, value); }
        }

        public AutoSourceSettingItem WarehouseShowCase
        {
            get { return GetProperty(() => WarehouseShowCase); }
            set { SetProperty(() => WarehouseShowCase, value); }
        }

        public AutoSourceSettingItem SetSourceMaxQuantity
        {
            get { return GetProperty(() => SetSourceMaxQuantity); }
            set { SetProperty(() => SetSourceMaxQuantity, value); }
        }

        public AutoSourceSettingItem OptimizePurchaseSources
        {
            get { return GetProperty(() => OptimizePurchaseSources); }
            set { SetProperty(() => OptimizePurchaseSources, value); }
        }

        public AutoSourceSettingItem SupplierNoDocumentsExtraChargeMinusPercent
        {
            get { return GetProperty(() => SupplierNoDocumentsExtraChargeMinusPercent); }
            set { SetProperty(() => SupplierNoDocumentsExtraChargeMinusPercent, value); }
        }

        public AutoSourceSettingItem SupplierBClassExtraChargeMinusPercent
        {
            get { return GetProperty(() => SupplierBClassExtraChargeMinusPercent); }
            set { SetProperty(() => SupplierBClassExtraChargeMinusPercent, value); }
        }

        public AutoSourceSettingItem MinExtraChargePercent
        {
            get { return GetProperty(() => MinExtraChargePercent); }
            set { SetProperty(() => MinExtraChargePercent, value); }
        }

        public AutoSourceSettingItem MaxPercentPurchasePrice
        {
            get { return GetProperty(() => MaxPercentPurchasePrice); }
            set { SetProperty(() => MaxPercentPurchasePrice, value); }
        }

        public AutoSourceSettingItem MaxInvoiceDaysArrive
        {
            get { return GetProperty(() => MaxInvoiceDaysArrive); }
            set { SetProperty(() => MaxInvoiceDaysArrive, value); }
        }

        public AutoSourceSettingItem OptimizationMinProfitDiff
        {
            get { return GetProperty(() => OptimizationMinProfitDiff); }
            set { SetProperty(() => OptimizationMinProfitDiff, value); }
        }

        public ReadOnlyObservableCollection<WarehouseKind> WarehouseTypes
        {
            get { return GetProperty(() => WarehouseTypes); }
            set { SetProperty(() => WarehouseTypes, value); }
        }

        public ReadOnlyObservableCollection<CarryType> CarryTypes
        {
            get { return GetProperty(() => CarryTypes); }
            private set { SetProperty(() => CarryTypes, value); }
        }

        public ObservableCollection<OrderProductSourceSettingItem> OrderProductSourceSettingItems
        {
            get { return GetProperty(() => OrderProductSourceSettingItems); }
            set { SetProperty(() => OrderProductSourceSettingItems, value); }
        }

        public ReadOnlyObservableCollection<OrderProductSourceEntityDto> OrderProductSourceEntities
        {
            get { return GetProperty(() => OrderProductSourceEntities); }
            set { SetProperty(() => OrderProductSourceEntities, value); }
        }

        #endregion

        #region Commands

        public IDelegateCommand AddOrderProductSourceSettingCommand { get; }

        public IDelegateCommand EditOrderProductSourceSettingCommand { get; }

        public IDelegateCommand RemoveOrderProductSourceSettingCommand { get; }

        #endregion

        #region DialogSettings

        public override int MinHeight => 550;

        public override int Height => 550;

        public override int MaxHeight => 1080;

        public override int MinWidth => 710;

        public override int Width => 790;

        public override int MaxWidth => 1920;

        #endregion

        private IErrorHandler ErrorHandler { get; }

        private IMapper Mapper { get; }

        protected override async Task HandleLoadedAsync()
        {
            WarehouseTypes = Dictionaries.GetItems<WarehouseKind>().ToReadOnlyObservableCollection();

            CarryTypes = Dictionaries.GetItems<CarryType>().ToReadOnlyObservableCollection();

            _autoSourceSettingDtos = await LoadAutoSourceSettingsAsync();
            _orderProductSourceSettingDtos = await LoadOrderProductSourceSettingsAsync();

            await LoadOrderProductSourcesAsync();

            MapAutoSourceSettingControls(_autoSourceSettingDtos);

            MapToOrderProductSourceSettingItems(_orderProductSourceSettingDtos);

            Title = "Настройки авто-источника";
        }

        protected override async Task HandleOkAsync()
        {
            IReadOnlyCollection<SourceSettingValueDto> sourceValueDtos = GetAutoSourceSettingChanging().ToReadOnlyObservableCollection();

            bool isOrderProductSourceSettingsChanged = IsOrderProductSourceSettingChanged();

            if (sourceValueDtos?.Count > 0 || isOrderProductSourceSettingsChanged)
            {
                if (MessageFacadeService.Confirm("Вы уверены?", "Сохранить настройки авто-источника"))
                {
                    bool isSaved = false;

                    if (sourceValueDtos?.Count > 0)
                    {
                        AutoSourceSettingsSaveDto saveDto = new AutoSourceSettingsSaveDto(sourceValueDtos);

                        Result result = await ErrorHandler.HandleErrorsAsync(
                            _ => WebClient.ExecuteApiRequestAsync(new SaveAutoSourceSettings(saveDto)),
                            "сохранении общих настроек авто-источника",
                            null,
                            this,
                            true);

                        if (result?.IsSuccess == true)
                        {
                            isSaved = true;
                        }
                    }

                    if (isOrderProductSourceSettingsChanged)
                    {
                        IReadOnlyCollection<OrderProductSourceSettingSaveDto> saveDtos = MapToOrderProductSourceSettingSaveDtos(OrderProductSourceSettingItems);

                        Result result = await ErrorHandler.HandleErrorsAsync(
                            _ => WebClient.ExecuteApiRequestAsync(new SaveOrderProductSourceSettings(saveDtos)),
                            "сохранении настроек авто-источника",
                            null,
                            this,
                            true);

                        if (result?.IsSuccess == true)
                        {
                            isSaved = true;
                        }
                    }

                    if (isSaved)
                    {
                        CloseOk();
                        MessageFacadeService.ShowNotificationInfo("Настройки авто-источника сохранены");

                        return;
                    }
                }

                return;
            }

            MessageFacadeService.ShowNotificationWarning("Нет изменений для сохранения");
        }

        private async Task<ReadOnlyObservableCollection<AutoSourceSettingDto>> LoadAutoSourceSettingsAsync()
        {
            List<AutoSourceSettingDto> autoSourceSettingDtos = await ErrorHandler.HandleErrorsAsync(
                _ => WebClient.ExecuteApiRequestAsync(new QueryAutoSourcesSettings()),
                "получении списка настоек авто-источника",
                null,
                this,
                true,
                showNotification: false);

            return autoSourceSettingDtos.ToReadOnlyObservableCollection();
        }

        private async Task<ReadOnlyObservableCollection<OrderProductSourceSettingDto>> LoadOrderProductSourceSettingsAsync()
        {
            List<OrderProductSourceSettingDto> orderProductSourceSettingDtos = await ErrorHandler.HandleErrorsAsync(
                _ => WebClient.ExecuteApiRequestAsync(new QueryOrderProductSourceSettings()),
                "получении настроек приоритетов",
                null,
                this,
                true,
                showNotification: false);

            return orderProductSourceSettingDtos?.ToReadOnlyObservableCollection();
        }

        private async Task LoadOrderProductSourcesAsync()
        {
            List<OrderProductSourceEntityDto> orderProductSourceEntityDto = await ErrorHandler.HandleErrorsAsync(
                _ => WebClient.ExecuteApiRequestAsync(new QueryOrderProductSourceEntities()),
                "получении списка источников",
                null,
                this,
                true,
                showNotification: false);

            OrderProductSourceEntities = orderProductSourceEntityDto?.ToReadOnlyObservableCollection();
        }

        private void MapAutoSourceSettingControls(ReadOnlyObservableCollection<AutoSourceSettingDto> autoSourceSettingDtos)
        {
            CurrentWeightKoef = Mapper.Map<AutoSourceSettingItem>(autoSourceSettingDtos.FirstOrDefault(x => x.Id == ConstantAutoSourceSettings.CurrentWeightKoef));
            SetSourceMaxQuantity = Mapper.Map<AutoSourceSettingItem>(autoSourceSettingDtos.FirstOrDefault(x => x.Id == ConstantAutoSourceSettings.SetSourceMaxQuantity));
            WarehouseShowCase = Mapper.Map<AutoSourceSettingItem>(autoSourceSettingDtos.FirstOrDefault(x => x.Id == ConstantAutoSourceSettings.WarehouseShowCase));
            OptimizePurchaseSources = Mapper.Map<AutoSourceSettingItem>(autoSourceSettingDtos.FirstOrDefault(x => x.Id == ConstantAutoSourceSettings.OptimizePurchaseSources));
            SupplierNoDocumentsExtraChargeMinusPercent = Mapper.Map<AutoSourceSettingItem>(autoSourceSettingDtos.FirstOrDefault(x => x.Id == ConstantAutoSourceSettings.SupplierNoDocumentsExtraChargeMinusPercent));
            SupplierBClassExtraChargeMinusPercent = Mapper.Map<AutoSourceSettingItem>(autoSourceSettingDtos.FirstOrDefault(x => x.Id == ConstantAutoSourceSettings.SupplierBClassExtraChargeMinusPercent));
            MinExtraChargePercent = Mapper.Map<AutoSourceSettingItem>(autoSourceSettingDtos.FirstOrDefault(x => x.Id == ConstantAutoSourceSettings.MinExtraChargePercent));
            MaxPercentPurchasePrice = Mapper.Map<AutoSourceSettingItem>(autoSourceSettingDtos.FirstOrDefault(x => x.Id == ConstantAutoSourceSettings.MaxPercentPurchasePrice));
            MaxInvoiceDaysArrive = Mapper.Map<AutoSourceSettingItem>(autoSourceSettingDtos.FirstOrDefault(x => x.Id == ConstantAutoSourceSettings.MaxInvoiceDaysArrive));
            OptimizationMinProfitDiff = Mapper.Map<AutoSourceSettingItem>(autoSourceSettingDtos.FirstOrDefault(x => x.Id == ConstantAutoSourceSettings.OptimizationMinProfitDiff));
        }

        private void MapToOrderProductSourceSettingItems(IReadOnlyCollection<OrderProductSourceSettingDto> orderProductSourceSettingDtos)
        {
            if (orderProductSourceSettingDtos?.Count > 0)
            {
                OrderProductSourceSettingItems = orderProductSourceSettingDtos.GroupBy(x => new { x.CarryId, x.SourceId, x.WarehouseTypeId })
                    .Select(group =>
                        MapToOrderProductSourceSettingItem(group.Key.CarryId, group.Key.SourceId, group.Key.WarehouseTypeId, group.ToArray()))
                    .ToObservableCollection();

            }

            if (OrderProductSourceSettingItems?.Any(x => x.CarryId == null) != true)
            {
                OrderProductSourceSettingItems ??= new ObservableCollection<OrderProductSourceSettingItem>();

                OrderProductSourceSettingItems.AddRange(CreateOrderProductSourceSettingItemByCarry(null));
            }
        }

        private OrderProductSourceSettingItem MapToOrderProductSourceSettingItem(
            int? carryId,
            int sourceId,
            int? warehouseTypeId,
            OrderProductSourceSettingDto[] productSourceSettingDtos)
        {
            return new OrderProductSourceSettingItem()
            {
                CarryId = carryId,
                CarryType = CarryTypes.FirstOrDefault(z => z.Id == carryId),
                SourceId = sourceId,
                WarehouseTypeId = warehouseTypeId,
                FullNameSource = CreateFullNameSource(sourceId, warehouseTypeId),
                ProductType = productSourceSettingDtos?.FirstOrDefault(x => x.ProductTypeId == ProductType.ProductId)?.Priority ?? 0,
                CertificateType = productSourceSettingDtos?.FirstOrDefault(x => x.ProductTypeId == ProductType.CertificateId)?.Priority ?? 0,
                ServiceCertificateType = productSourceSettingDtos?.FirstOrDefault(x => x.ProductTypeId == ProductType.ServiceCertificateId)?.Priority ?? 0,
                AssemblyServiceType = productSourceSettingDtos?.FirstOrDefault(x => x.ProductTypeId == ProductType.AssemblyServiceId)?.Priority ?? 0,
                AccessoryType = productSourceSettingDtos?.FirstOrDefault(x => x.ProductTypeId == ProductType.AccessoryId)?.Priority ?? 0,
                AssembledComputerRuleType = productSourceSettingDtos?.FirstOrDefault(x => x.ProductTypeId == ProductType.AssembledComputerRuleId)?.Priority ?? 0,
                ProductInAssemblyNotCollectType = productSourceSettingDtos?.FirstOrDefault(x => x.ProductTypeId == ProductType.ProductInAssemblyNotCollectId)?.Priority ?? 0,
                ProductInAssemblyCollectType = productSourceSettingDtos?.FirstOrDefault(x => x.ProductTypeId == ProductType.ProductInAssemblyCollectId)?.Priority ?? 0,
                ProductInAssemblyCollectForconfigurationType = productSourceSettingDtos?.FirstOrDefault(x => x.ProductTypeId == ProductType.ProductInAssemblyCollectForconfigurationId)?.Priority ?? 0,
                ProductWithServiceType = productSourceSettingDtos?.FirstOrDefault(x => x.ProductTypeId == ProductType.ProductWithServiceId)?.Priority ?? 0,
                TradeInType = productSourceSettingDtos?.FirstOrDefault(x => x.ProductTypeId == ProductType.TradeInId)?.Priority ?? 0,
                DiscountType = productSourceSettingDtos?.FirstOrDefault(x => x.ProductTypeId == ProductType.DiscountId)?.Priority ?? 0,
                RefType = productSourceSettingDtos?.FirstOrDefault(x => x.ProductTypeId == ProductType.RefId)?.Priority ?? 0,
                SecondHandType = productSourceSettingDtos?.FirstOrDefault(x => x.ProductTypeId == ProductType.SecondHandId)?.Priority ?? 0,
                AdditionalServiceConsumableType = productSourceSettingDtos?.FirstOrDefault(x => x.ProductTypeId == ProductType.ProductAdditionalServiceConsumableId)?.Priority ?? 0
            };
        }

        private IReadOnlyCollection<OrderProductSourceSettingSaveDto> MapToOrderProductSourceSettingSaveDtos(IReadOnlyCollection<OrderProductSourceSettingItem> orderProductSourceSettingItems)
        {
            List<OrderProductSourceSettingSaveDto> result = new List<OrderProductSourceSettingSaveDto>();

            foreach (OrderProductSourceSettingItem item in orderProductSourceSettingItems)
            {
                result.Add(new OrderProductSourceSettingSaveDto(item.SourceId, ProductType.ProductId, item.CarryId, item.WarehouseTypeId, item.ProductType));
                result.Add(new OrderProductSourceSettingSaveDto(item.SourceId, ProductType.CertificateId, item.CarryId, item.WarehouseTypeId, item.CertificateType));
                result.Add(new OrderProductSourceSettingSaveDto(item.SourceId, ProductType.ServiceCertificateId, item.CarryId, item.WarehouseTypeId, item.ServiceCertificateType));
                result.Add(new OrderProductSourceSettingSaveDto(item.SourceId, ProductType.AssemblyServiceId, item.CarryId, item.WarehouseTypeId, item.AssemblyServiceType));
                result.Add(new OrderProductSourceSettingSaveDto(item.SourceId, ProductType.AccessoryId, item.CarryId, item.WarehouseTypeId, item.AccessoryType));
                result.Add(new OrderProductSourceSettingSaveDto(item.SourceId, ProductType.AssembledComputerRuleId, item.CarryId, item.WarehouseTypeId, item.AssembledComputerRuleType));
                result.Add(new OrderProductSourceSettingSaveDto(item.SourceId, ProductType.ProductInAssemblyNotCollectId, item.CarryId, item.WarehouseTypeId, item.ProductInAssemblyNotCollectType));
                result.Add(new OrderProductSourceSettingSaveDto(item.SourceId, ProductType.ProductInAssemblyCollectId, item.CarryId, item.WarehouseTypeId, item.ProductInAssemblyCollectType));
                result.Add(new OrderProductSourceSettingSaveDto(item.SourceId, ProductType.ProductInAssemblyCollectForconfigurationId, item.CarryId, item.WarehouseTypeId, item.ProductInAssemblyCollectForconfigurationType));
                result.Add(new OrderProductSourceSettingSaveDto(item.SourceId, ProductType.ProductWithServiceId, item.CarryId, item.WarehouseTypeId, item.ProductWithServiceType));
                result.Add(new OrderProductSourceSettingSaveDto(item.SourceId, ProductType.TradeInId, item.CarryId, item.WarehouseTypeId, item.TradeInType));
                result.Add(new OrderProductSourceSettingSaveDto(item.SourceId, ProductType.DiscountId, item.CarryId, item.WarehouseTypeId, item.DiscountType));
                result.Add(new OrderProductSourceSettingSaveDto(item.SourceId, ProductType.RefId, item.CarryId, item.WarehouseTypeId, item.RefType));
                result.Add(new OrderProductSourceSettingSaveDto(item.SourceId, ProductType.SecondHandId, item.CarryId, item.WarehouseTypeId, item.SecondHandType));
                result.Add(new OrderProductSourceSettingSaveDto(item.SourceId, ProductType.ProductAdditionalServiceConsumableId, item.CarryId, item.WarehouseTypeId, item.AdditionalServiceConsumableType));
            }

            return result;
        }

        private IEnumerable<SourceSettingValueDto> GetAutoSourceSettingChanging()
        {
            if (IsChangeValue(CurrentWeightKoef.Value, ConstantAutoSourceSettings.CurrentWeightKoef))
            {
                yield return new SourceSettingValueDto(CurrentWeightKoef.Id, CurrentWeightKoef.Value);
            }

            if (IsChangeValue(SetSourceMaxQuantity.Value, ConstantAutoSourceSettings.SetSourceMaxQuantity))
            {
                yield return new SourceSettingValueDto(SetSourceMaxQuantity.Id, SetSourceMaxQuantity.Value);
            }

            if (IsChangeValue(WarehouseShowCase.Value, ConstantAutoSourceSettings.WarehouseShowCase))
            {
                yield return new SourceSettingValueDto(WarehouseShowCase.Id, WarehouseShowCase.Value);
            }

            if (IsChangeValue(OptimizePurchaseSources.Value, ConstantAutoSourceSettings.OptimizePurchaseSources))
            {
                yield return new SourceSettingValueDto(OptimizePurchaseSources.Id, OptimizePurchaseSources.Value);
            }

            if (IsChangeValue(SupplierNoDocumentsExtraChargeMinusPercent.Value, ConstantAutoSourceSettings.SupplierNoDocumentsExtraChargeMinusPercent))
            {
                yield return new SourceSettingValueDto(SupplierNoDocumentsExtraChargeMinusPercent.Id, SupplierNoDocumentsExtraChargeMinusPercent.Value);
            }

            if (IsChangeValue(SupplierBClassExtraChargeMinusPercent.Value, ConstantAutoSourceSettings.SupplierBClassExtraChargeMinusPercent))
            {
                yield return new SourceSettingValueDto(SupplierBClassExtraChargeMinusPercent.Id, SupplierBClassExtraChargeMinusPercent.Value);
            }

            if (IsChangeValue(MinExtraChargePercent.Value, ConstantAutoSourceSettings.MinExtraChargePercent))
            {
                yield return new SourceSettingValueDto(MinExtraChargePercent.Id, MinExtraChargePercent.Value);
            }

            if (IsChangeValue(MaxPercentPurchasePrice.Value, ConstantAutoSourceSettings.MaxPercentPurchasePrice))
            {
                yield return new SourceSettingValueDto(MaxPercentPurchasePrice.Id, MaxPercentPurchasePrice.Value);
            }

            if (IsChangeValue(OptimizationMinProfitDiff.Value, ConstantAutoSourceSettings.OptimizationMinProfitDiff))
            {
                yield return new SourceSettingValueDto(OptimizationMinProfitDiff.Id, OptimizationMinProfitDiff.Value);
            }

            if (IsChangeValue(MaxInvoiceDaysArrive.Value, ConstantAutoSourceSettings.MaxInvoiceDaysArrive))
            {
                yield return new SourceSettingValueDto(MaxInvoiceDaysArrive.Id, MaxInvoiceDaysArrive.Value);
            }
        }

        private bool IsChangeValue(string newValue, int sourceSettingsId)
        {
            AutoSourceSettingDto autoSourceSettingDto = _autoSourceSettingDtos?.FirstOrDefault(x => x.Id == sourceSettingsId);

            return newValue != autoSourceSettingDto?.Value;
        }

        private string CreateFullNameSource(int sourceId, int? warehouseTypeId)
        {
            string nameSourse = OrderProductSourceEntities?.FirstOrDefault(x => x.Id == sourceId)?.Name;

            if (sourceId == SourceTypes.Warehouse)
            {
                string warehouseTypeName = WarehouseTypes?.FirstOrDefault(x => x.Id == warehouseTypeId)?.Name;

                return string.IsNullOrEmpty(warehouseTypeName) ? $"{nameSourse}" : $"{nameSourse} ({warehouseTypeName})";
            }

            if (sourceId == SourceTypes.Transit || sourceId == SourceTypes.Purchase)
            {
                return $"Закупка ({nameSourse})";
            }

            return $"Перемещение ({nameSourse})";
        }

        private ReadOnlyObservableCollection<OrderProductSourceSettingItem> CreateOrderProductSourceSettingItemByCarry(int? carryId)
        {
            List<OrderProductSourceSettingItem> orderProductSourceSettingItems = new List<OrderProductSourceSettingItem>();

            foreach (OrderProductSourceEntityDto orderProductSourceEntity in OrderProductSourceEntities)
            {
                if (orderProductSourceEntity.Id == SourceTypes.Warehouse)
                {
                    WarehouseTypes.ForEach(
                        x =>
                        {
                            OrderProductSourceSettingItem warehouseOrderProductSourceSettingItem = MapToOrderProductSourceSettingItem(
                                carryId,
                                orderProductSourceEntity.Id,
                                x.Id,
                                null);

                            orderProductSourceSettingItems.Add(warehouseOrderProductSourceSettingItem);
                        });

                    continue;
                }

                OrderProductSourceSettingItem orderProductSourceSettingItem = MapToOrderProductSourceSettingItem(
                    carryId,
                    orderProductSourceEntity.Id,
                    null,
                    null);

                orderProductSourceSettingItems.Add(orderProductSourceSettingItem);
            }

            return orderProductSourceSettingItems.ToReadOnlyObservableCollection();
        }

        private void AddOrderProductSourceSetting()
        {
            int[] ignoreCarryIds = OrderProductSourceSettingItems?
                .Where(x => x.CarryId.HasValue)
                .GroupBy(x => x.CarryId.Value)
                .Select(x => x.Key)
                .ToArray();

            SelectCarryParameter parameter = new SelectCarryParameter(ignoreCarryIds);

            SelectCarryViewModel model = DialogDocumentManagerService.ShowView<SelectCarryViewModel>(parameter, this);

            if (model.IsOk)
            {
                OrderProductSourceSettingItems ??= new ObservableCollection<OrderProductSourceSettingItem>();

                OrderProductSourceSettingItems.AddRange(CreateOrderProductSourceSettingItemByCarry(model.CarryId));
            }
        }

        private void EditOrderProductSourceSetting(OrderProductSourceSettingItem item)
        {
            int? oldCarryId = item.CarryId;

            int[] ignoreCarryIds = OrderProductSourceSettingItems
                .Where(x => x.CarryId.HasValue)
                .GroupBy(x => x.CarryId.Value)
                .Select(x => x.Key)
                .ToArray();

            ChangeCarryParameter parameter = new ChangeCarryParameter(oldCarryId ?? 0, ignoreCarryIds);

            ChangeCarryViewModel model = DialogDocumentManagerService.ShowView<ChangeCarryViewModel>(parameter, this);

            if (model.IsOk)
            {
                CarryType carryType = CarryTypes.FirstOrDefault(z => z.Id == model.NewCarryId);

                OrderProductSourceSettingItems.Where(x => x.CarryId == oldCarryId).ForEach(x =>
                {
                    x.CarryId = model.NewCarryId;
                    x.CarryType = carryType;
                });

                if (!oldCarryId.HasValue)
                {
                    OrderProductSourceSettingItems.AddRange(CreateOrderProductSourceSettingItemByCarry(null));
                }
            }
        }

        private void RemoveOrderProductSourceSetting(OrderProductSourceSettingItem item)
        {
            if (!item.CarryId.HasValue)
            {
                MessageFacadeService.ShowMessageBoxInfo("Нельзя удалить настройки по умалчанию");
                return;
            }

            OrderProductSourceSettingItem[] deleteItems = OrderProductSourceSettingItems.Where(x => x.CarryId == item.CarryId).ToArray();

            OrderProductSourceSettingItems.Remove(deleteItems);
        }

        private bool IsOrderProductSourceSettingChanged()
        {
            OrderProductSourceSettingItem[] oldOrderProductSourceSettingItems = _orderProductSourceSettingDtos.GroupBy(x => new { x.CarryId, x.SourceId, x.WarehouseTypeId })
                .Select(group => MapToOrderProductSourceSettingItem(group.Key.CarryId, group.Key.SourceId, group.Key.WarehouseTypeId, group.ToArray()))
                .ToArray();

            if (oldOrderProductSourceSettingItems.Length != OrderProductSourceSettingItems?.Count)
            {
                return true;
            }

            foreach (OrderProductSourceSettingItem item in OrderProductSourceSettingItems)
            {
                OrderProductSourceSettingItem oldItem = oldOrderProductSourceSettingItems
                    .FirstOrDefault(x => x.CarryId == item.CarryId && x.SourceId == item.SourceId && x.WarehouseTypeId == item.WarehouseTypeId);

                if (oldItem == null || !oldItem.Equals(item))
                {
                    return true;
                }
            }

            return false;
        }
    }
}