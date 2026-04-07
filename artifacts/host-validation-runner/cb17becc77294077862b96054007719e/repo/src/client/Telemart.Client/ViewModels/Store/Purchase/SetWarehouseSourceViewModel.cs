using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using Microsoft.Extensions.Logging;
using Telemart.Client.Business;
using Telemart.Client.Common;
using Telemart.Client.Common.Messages;
using Telemart.Client.Common.Services;
using Telemart.Client.Core.ErrorHandling;
using Telemart.Client.Core.Exceptions;
using Telemart.Client.Data.Requests.Features.Purchase;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Data.WebClient.Security;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.TransferObjects;
using Telemart.Client.ViewModels.Base;
using Telemart.Client.ViewModels.Common;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.ViewModels.Store.Purchase
{
    internal sealed class SetWarehouseSourceViewModel : TelemartDialogViewModelBase
    {
        private SetSourceParameter data;

        public SetWarehouseSourceViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            IMessenger messenger)
            : base(webClient, dictionaries, messageFacadeService)
        {
            Messenger = messenger;
            HandlePreviewKeyDownCommand = new DelegateCommand<KeyEventArgs>(HandlePreviewKeyDown);
        }

        public SetWarehouseSourceViewModel()
        {
        }

        #region Commands

        public IDelegateCommand HandlePreviewKeyDownCommand { get; }

        #endregion

        #region Dependency properties

        public ObservableCollection<SetWarehouseSourceViewItem> Leftovers
        {
            get { return GetProperty(() => Leftovers); }
            private set { SetProperty(() => Leftovers, value); }
        }

        public SetWarehouseSourceViewItem SelectedLeftover
        {
            get { return GetProperty(() => SelectedLeftover); }
            set { SetProperty(() => SelectedLeftover, value); }
        }

        public IEnumerable<SummaryViewItem> SummaryItems
        {
            get { return GetProperty(() => SummaryItems); }
            private set { SetProperty(() => SummaryItems, value); }
        }

        public string ProductName
        {
            get { return GetProperty(() => ProductName); }
            private set { SetProperty(() => ProductName, value); }
        }

        #endregion

        #region DialogSettings

        public override int Height => 450;

        public override int MinHeight => 200;

        public override int MinWidth => 600;

        public override int Width => 800;

        #endregion

        private IMessenger Messenger { get; }

        protected override bool CanOk()
        {
            return SelectedLeftover != null;
        }

        protected override Task HandleLoadedAsync()
        {
            object[] p = (object[])Parameter;

            data = (SetSourceParameter)p[0];
            IEnumerable<PurchaseWarehouseSourceDto> sources = (IEnumerable<PurchaseWarehouseSourceDto>)p[1];

            SummaryItems = GetItems();

            ProductName = data.ProductName;

            Leftovers = sources
                .Select(x => ToViewItem(x, data.Quantity, data.OrderDeliveryTime, data.OrderState))
                .OrderByDescending(x => x.SatisfyNeeds)
                .ThenByDescending(x => x.WarehousePosition)
                .ThenBy(x => x.WarehouseName)
                .ToObservableCollection();

            Title = "Выбор склада";

            static SetWarehouseSourceViewItem ToViewItem(PurchaseWarehouseSourceDto source, int quantity, DateTime? orderDeliveryDateTime, OrderStatus orderState) => new SetWarehouseSourceViewItem
            {
                WarehouseId = source.WarehouseId,
                WarehouseName = source.WarehouseName,
                ReservedQuantity = source.ReservedQuantity,
                WarehouseItems = source.WarehouseItems,
                WarehousePosition = source.WarehousePosition,
                Needs = quantity,
                DeliveryDateTime = source.DeliveryDateTime,
                OrderDeliveryDateTime = orderDeliveryDateTime,
                OrderState = orderState
            };

            return Task.CompletedTask;
        }

        protected override async Task HandleOkAsync()
        {
            if (!SelectedLeftover.SatisfyNeeds)
            {
               MessageFacadeService.ShowNotificationWarning($"Товара нет в свободном остатке {data.Quantity.ToString(CultureInfo.InvariantCulture)} шт.");
               return;
            }

            try
            {
                PurchaseDto purchaseFromServer = await WebClient.ExecuteApiRequestAsync(new UpdatePurchaseSource(data.ProductRecordId, GetPurchaseSaveSource(null)));

                Messenger.Send(new PurchaseMessage(purchaseFromServer, MessageType.Changed));

                MessageFacadeService.ShowNotificationInfo("Источник успешно установлен");

                IsOk = true;
                Close();
            }
            catch (UnexpectedSatusException exception)
            {
                if (data.OrderPaymentId == Payment.CashlessTaxId
                    && WebClient.IsOperationAllowed(BusinessOperation.AutoSourceWithoutWhiteOnly)
                    && exception.Args?.Error?.ErrorCode == ErrorCode.SetSourceInsufficientQuantity)
                {
                    await TrySetSourceWithoutWhiteStockAsync();
                }
                else
                {
                    MessageFacadeService.ShowNotificationError(exception.Args?.Error.GetErrorMessage());
                }
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Failed to set purchase source");
                MessageFacadeService.ShowNotificationError("Ошибка установки источника");
            }
        }

        protected override void OnInitializeInDesignMode()
        {
            base.OnInitializeInDesignMode();

            data = new SetSourceParameter
            {
                Quantity = 1000,
                Price = 100,
                CurrencyId = Currency.Uah.Id,
                OrderWarehouseName = "ул.Довженко Магазин (Телемарт)",
                OrderDeliveryTime = DateTime.Now,
                OrderProductState = OrderProductStatus.New
            };

            SummaryItems = GetItems();
        }

        private IEnumerable<SummaryViewItem> GetItems()
        {
            yield return new SummaryViewItem("Кол-во", $"{data.Quantity:D} шт.");
            yield return new SummaryViewItem("Цена", CurrencyFormatingRules.ToStr(data.Price, data.CurrencyId));
            yield return new SummaryViewItem("Склад", data.OrderWarehouseName);
            yield return new SummaryViewItem("Дата Х", data.OrderDeliveryTime?.ToString(DateFormattingRules.FullDateTimeFormat));
            yield return new SummaryViewItem("Статус", data.OrderProductState.Name);
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

        private PurchaseSourceSaveDto GetPurchaseSaveSource(bool? withoutStockWhite)
        {
            return PurchaseSourceSaveDto.Warehouse(SelectedLeftover.WarehouseId, SelectedLeftover.WarehouseName, withoutStockWhite);
        }

        private async Task TrySetSourceWithoutWhiteStockAsync()
        {
            try
            {
                DelayedConfirmViewModel viewModel = DialogDocumentManagerService.ShowView<DelayedConfirmViewModel>(
                    "Товаров нет на свободном остатке для данного способа оплаты. Продолжить установку источника с выбранного склада?",
                    this);

                if (!viewModel.IsOk)
                {
                    return;
                }

                PurchaseDto purchaseFromServer = await WebClient.ExecuteApiRequestAsync(new UpdatePurchaseSource(data.ProductRecordId, GetPurchaseSaveSource(true)));

                Messenger.Send(new PurchaseMessage(purchaseFromServer, MessageType.Changed));

                MessageFacadeService.ShowNotificationInfo("Источник успешно установлен");

                IsOk = true;
                Close();
            }
            catch (UnexpectedSatusException exception)
            {
                MessageFacadeService.ShowNotificationError(exception.Args.Error.GetErrorMessage());
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Failed to set purchase source");
                MessageFacadeService.ShowNotificationError("Ошибка установки источника");
            }
        }
    }
}