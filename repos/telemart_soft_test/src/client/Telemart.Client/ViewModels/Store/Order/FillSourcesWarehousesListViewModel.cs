using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using Microsoft.Extensions.Logging;
using Telemart.Client.Common.MvvmEnhancements.Grid;
using Telemart.Client.Common.Services;
using Telemart.Client.Core.Exceptions;
using Telemart.Client.Data.Requests.Features.Order;
using Telemart.Client.Data.Requests.Features.Warehouse;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.Properties;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.Warehouse;
using Telemart.Client.ViewModels.Base;
using Telemart.Client.ViewModels.Validation;
using Telemart.Client.ViewModels.Warehouse;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.ViewModels.Store.Order
{
    public sealed class FillSourcesWarehousesListViewModel : TelemartDialogViewModelBase
    {
        private FillSourcesWarehousesListParameter parameter;

        public FillSourcesWarehousesListViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            IMapper mapper)
            : base(webClient, dictionaries, messageFacadeService)
        {
            Mapper = mapper;

            MoveUpCommand = new DelegateCommand<CheckableItem<WarehouseViewItem>>(MoveUp, x => x != null);
            MoveDownCommand = new DelegateCommand<CheckableItem<WarehouseViewItem>>(MoveDown, x => x != null);
        }

        public FillSourcesWarehousesListViewModel()
        {
        }

        public IDelegateCommand MoveUpCommand { get; }

        public IDelegateCommand MoveDownCommand { get; }

        public ObservableCollection<CheckableItem<WarehouseViewItem>> Warehouses
        {
            get { return GetProperty(() => Warehouses); }
            private set { SetProperty(() => Warehouses, value); }
        }

        public bool UseInvoices
        {
            get { return GetProperty(() => UseInvoices); }
            set { SetProperty(() => UseInvoices, value); }
        }

        public Result<OrderDto> Result { get; private set; }

        private IMapper Mapper { get; }

        protected override async Task HandleLoadedAsync()
        {
            parameter = (FillSourcesWarehousesListParameter)Parameter;

            List<WarehouseDto> warehouses = await WebClient.ExecuteApiRequestAsync(new QueryWarehouses(), true).GetPagedResultDataAsync();

            WarehouseDto orderWarehouse = warehouses.FirstOrDefault(x => x.Id == parameter.OrderWarehouseId);

            UseInvoices = true;

            Warehouses = warehouses
                .Where(x => x.Active == 1 && (x.TypeId == WarehouseKind.Main.Id || x.TypeId == WarehouseKind.Pickup.Id))
                .OrderByDescending(x => x.Id == orderWarehouse?.Id)
                .ThenBy(x => x.TypeId)
                .ThenByDescending(x => x.CityId == orderWarehouse?.CityId)
                .ThenByDescending(x => x.Position)
                .Select(x => new CheckableItem<WarehouseViewItem>(Mapper.Map<WarehouseViewItem>(x), x.Id == parameter.OrderWarehouseId || x.TypeId == WarehouseKind.Main.Id))
                .ToObservableCollection();

            Title = "Выбор склада";
        }

        protected override async Task HandleOkAsync()
        {
            if (!Warehouses.Any(x => x.IsChecked))
            {
                MessageFacadeService.ShowNotificationWarning("Выберите хотя бы один склад");
                return;
            }

            try
            {
                FillOrderSources request = new FillOrderSources(
                    parameter.OrderId,
                    Warehouses.Where(x => x.IsChecked).Select(x => x.Item.Id).ToArray(),
                    UseInvoices,
                    false);

                Result = await WebClient.ExecuteApiRequestAsync(request);

                if (Result.Warnings.Any())
                {
                    MessageFacadeService.ShowNotificationWarning("Источники установлены c предупреждениями");
                    ShowValidationResultView("Предупрежедения", Result.Warnings.Select(x => new ValidationResultItem(x, false)).ToList());
                }
                else
                {
                    MessageFacadeService.ShowNotificationInfo("Источники успешно установлены");
                }

                IsOk = true;
                Close();
            }
            catch (UnexpectedSatusException exception)
            {
                IReadOnlyCollection<ValidationResultItem> validationItems = exception.GetErrorItems();

                if (validationItems.Any(x => x.IsError))
                {
                    MessageFacadeService.ShowNotificationError("Ошибка при установке источников");
                    ShowValidationResultView("Ошибки", validationItems);
                }
                else
                {
                    MessageFacadeService.ShowNotificationWarning(string.Join(Environment.NewLine, validationItems.Select(x => x.Message)));
                }
            }
            catch (UnexpectedErrorException)
            {
                MessageFacadeService.ShowNotificationError("Ошибка при установке источников");
                ShowValidationResultView("Ошибки", new[] { new ValidationResultItem(Resources.ServerUnavailable, true) });
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Failed to fill order sources");
                MessageFacadeService.ShowNotificationError("Ошибка при установке источников");
            }
        }

        private void MoveUp(CheckableItem<WarehouseViewItem> item)
        {
            int index = Warehouses.IndexOf(item);

            if (index > 0)
            {
                Warehouses.Move(index, index - 1);
            }
        }

        private void MoveDown(CheckableItem<WarehouseViewItem> item)
        {
            int index = Warehouses.IndexOf(item);

            if (index < Warehouses.Count - 1)
            {
                Warehouses.Move(index, index + 1);
            }
        }
    }
}