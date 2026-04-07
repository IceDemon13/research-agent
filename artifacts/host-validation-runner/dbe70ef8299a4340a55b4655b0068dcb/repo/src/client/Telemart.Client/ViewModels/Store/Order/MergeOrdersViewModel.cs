using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using DevExpress.Mvvm.Native;
using Microsoft.Extensions.Logging;
using Telemart.Client.Common;
using Telemart.Client.Common.Services;
using Telemart.Client.Core.Exceptions;
using Telemart.Client.Data.Requests.Features.City;
using Telemart.Client.Data.Requests.Features.Contractor;
using Telemart.Client.Data.Requests.Features.Order;
using Telemart.Client.Data.Requests.Features.Warehouse;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.Properties;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.City;
using Telemart.Client.TransferObjects.Warehouse;
using Telemart.Client.ViewModels.Base;
using Telemart.Client.ViewModels.Validation;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.ViewModels.Store.Order
{
    public sealed class MergeOrdersViewModel : TelemartDialogViewModelBase
    {
        private OrderDto order;

        private IReadOnlyDictionary<int, string> cities;
        private IReadOnlyDictionary<int, string> warehouses;
        private IReadOnlyDictionary<int, string> contractors;

        public MergeOrdersViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService)
            : base(webClient, dictionaries, messageFacadeService)
        {
            SelectedOrdersToMerge = new ObservableCollection<OrderDto>();
        }

        public MergeOrdersViewModel()
        {
        }

        #region INPC

        public ObservableCollection<OrderDto> OrdersToMerge
        {
            get { return GetProperty(() => OrdersToMerge); }
            set { SetProperty(() => OrdersToMerge, value); }
        }

        public ObservableCollection<OrderDto> SelectedOrdersToMerge
        {
            get { return GetProperty(() => SelectedOrdersToMerge); }
            set { SetProperty(() => SelectedOrdersToMerge, value); }
        }

        public IEnumerable<SummaryViewItem> OrderSummaryItems
        {
            get { return GetProperty(() => OrderSummaryItems); }
            private set { SetProperty(() => OrderSummaryItems, value); }
        }

        public string HelpContent
        {
            get { return GetProperty(() => HelpContent); }
            set { SetProperty(() => HelpContent, value); }
        }

        #endregion

        protected override async Task HandleLoadedAsync()
        {
            MergeOrdersParameter mergeParameter = (MergeOrdersParameter)Parameter;

            order = mergeParameter.TargetOrder;
            OrdersToMerge = mergeParameter.OrdersToMerge.ToObservableCollection();

            await Task.WhenAll(RefreshWarehouses(), RefreshContractors(), RefreshCities());

            OrderSummaryItems = GetOrderSummaryItems();

            HelpContent = $"Данный модуль переносит товары из отмеченных заказов в заказ №{order.Id}";

            Title = "Объединение заказов";

            async Task RefreshWarehouses()
            {
                List<WarehouseDto> warehousesList = await WebClient.ExecuteApiRequestAsync(new QueryWarehouses(), true).GetPagedResultDataAsync();
                warehouses = warehousesList.ToDictionary(x => x.Id, x => x.Name);
            }

            async Task RefreshContractors()
            {
                List<ContractorDto> contractorsList = await WebClient.ExecuteApiRequestAsync(new QueryContractors(), true).GetPagedResultDataAsync();
                contractors = contractorsList.ToDictionary(x => x.Id, x => x.Name);
            }

            async Task RefreshCities()
            {
                List<CityDto> citiesList = await WebClient.ExecuteApiRequestAsync(new QueryCities(), true).GetPagedResultDataAsync();
                cities = citiesList.ToDictionary(x => x.Id, x => x.Name);
            }
        }

        protected override async Task HandleOkAsync()
        {
            try
            {
                if (SelectedOrdersToMerge.Any())
                {
                    Dictionary<int, OrderFolderDto> folders = order.Folders.Union(SelectedOrdersToMerge.SelectMany(x => x.Folders)).ToDictionary(x => x.Id);

                    if ((folders.Any()
                         && (order.Products.Any(x =>
                                 x.OrderFolderId.HasValue
                                 && (folders[x.OrderFolderId.Value].TypeId == OrderFolderType.AssemblyServiceId || folders[x.OrderFolderId.Value].TypeId == OrderFolderType.AssembledComputerRuleId))
                             && SelectedOrdersToMerge.Any(x => x.Products.Any(y => y.OrderFolderId.HasValue && (folders[y.OrderFolderId.Value].TypeId == OrderFolderType.AssemblyServiceId || folders[y.OrderFolderId.Value].TypeId == OrderFolderType.AssembledComputerRuleId)))))
                        || SelectedOrdersToMerge.Count(x => x.Products.Any(y => y.OrderFolderId.HasValue)) > 1)
                    {
                        MessageFacadeService.ShowNotificationWarning("Запрещено объединять заказы, если в обоих есть сборка ПК");
                        return;
                    }

                    int[] orderIds = SelectedOrdersToMerge.Select(x => x.Id).ToArray();

                    MergeOrder gatewayRequest = new MergeOrder(order.Id, new OrderMergeDto { Id = order.Id, SourceIds = orderIds });

                    Result<OrderDto> result = await WebClient.ExecuteApiRequestAsync(gatewayRequest);

                    MessageFacadeService.ShowNotificationInfo($"Объединение заказов в заказ №{result.Data.Id} прошло успешно");

                    IsOk = true;
                    Close();
                }
                else
                {
                    MessageFacadeService.ShowNotificationWarning("Выберите хотя бы один заказ");
                }
            }
            catch (UnexpectedSatusException exception)
            {
                MessageFacadeService.ShowNotificationError("Ошибка при объединении заказов");
                ShowValidationResultView("Ошибки при объединении заказов", exception.GetErrorItems());
            }
            catch (UnexpectedErrorException)
            {
                MessageFacadeService.ShowNotificationError("Ошибка при объединении заказов");
                ShowValidationResultView("Ошибки при объединении заказов", new[] { new ValidationResultItem(Resources.ServerUnavailable, true) });
            }
            catch (Exception exception)
            {
                MessageFacadeService.ShowNotificationError("Ошибка при объединении заказов");
                Logger.LogError(exception, "Failed to merge orders. Base order id: {OrderId}", order.Id);
            }
        }

        private IEnumerable<SummaryViewItem> GetOrderSummaryItems()
        {
            yield return new SummaryViewItem("Заказ", order.Id.ToString());
            yield return new SummaryViewItem("Контрагент", contractors.GetValueOrDefault(order.ClientId, string.Empty));
            yield return new SummaryViewItem("Город", cities.GetValueOrDefault(order.CityId ?? 0, string.Empty));
            yield return new SummaryViewItem("Доставка", Dictionaries.GetItemById<CarryType>(order.CarryId).Name);
            yield return new SummaryViewItem("Склад", warehouses.GetValueOrDefault(order.WarehouseId ?? 0, string.Empty));
            yield return new SummaryViewItem("Оплата", Dictionaries.GetItemById<Payment>(order.PaymentId).Name);
        }
    }
}