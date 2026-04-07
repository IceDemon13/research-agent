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
using Telemart.Client.Dictionaries;
using Telemart.Client.TransferObjects;
using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.Store.Purchase
{
    internal sealed class SetMovementSourceViewModel : TelemartDialogViewModelBase
    {
        private SetSourceParameter data;

        public SetMovementSourceViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            IMessenger messenger)
            : base(webClient, dictionaries, messageFacadeService)
        {
            Messenger = messenger;
            HandlePreviewKeyDownCommand = new DelegateCommand<KeyEventArgs>(HandlePreviewKeyDown);
        }

        public SetMovementSourceViewModel()
        {
        }

        #region Commands

        public IDelegateCommand HandlePreviewKeyDownCommand { get; }

        #endregion

        #region Dependency properties

        public ObservableCollection<SetMovementSourceViewItem> Movements
        {
            get { return GetProperty(() => Movements); }
            private set { SetProperty(() => Movements, value); }
        }

        public SetMovementSourceViewItem SelectedMovement
        {
            get { return GetProperty(() => SelectedMovement); }
            set { SetProperty(() => SelectedMovement, value); }
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
            return SelectedMovement != null;
        }

        protected override Task HandleLoadedAsync()
        {
            object[] p = (object[])Parameter;

            data = (SetSourceParameter)p[0];
            IEnumerable<PurchaseMovementSourceDto> sources = (IEnumerable<PurchaseMovementSourceDto>)p[1];

            SummaryItems = GetItems();

            ProductName = data.ProductName;

            Movements = sources
                .Select(x => ToViewItem(x, data.Quantity, data.OrderDeliveryTime, data.OrderState))
                .OrderByDescending(x => x.SatisfyNeeds)
                .ThenBy(x => x.WarehouseToName)
                .ToObservableCollection();

            Title = "Выбор перемещения";

            return Task.CompletedTask;
        }

        protected override async Task HandleOkAsync()
        {
            if (!SelectedMovement.SatisfyNeeds)
            {
                MessageFacadeService.ShowNotificationWarning($"Товара нет в свободном остатке {data.Quantity.ToString(CultureInfo.InvariantCulture)} шт.");
                return;
            }

            try
            {
                PurchaseDto purchaseFromServer = await WebClient.ExecuteApiRequestAsync(new UpdatePurchaseSource(data.ProductRecordId, GetPurchaseSaveSource()));

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

        protected override void OnInitializeInDesignMode()
        {
            base.OnInitializeInDesignMode();

            data = new SetSourceParameter
            {
                Quantity = 13,
                Price = 3773,
                CurrencyId = Currency.Uah.Id,
                OrderWarehouseName = "ул.Довженко Магазин (Телемарт)",
                OrderDeliveryTime = DateTime.Now,
                OrderProductState = OrderProductStatus.New
            };

            SummaryItems = GetItems();
        }

        private static SetMovementSourceViewItem ToViewItem(PurchaseMovementSourceDto source, int quantity, DateTime? orderDeliveryDateTime, OrderStatus orderState)
        {
            return new SetMovementSourceViewItem
            {
                MovementId = source.Id,
                WarehouseFromId = source.WarehouseFromId,
                WarehouseFromName = source.WarehouseFromName,
                WarehouseToId = source.WarehouseToId,
                WarehouseToName = source.WarehouseToName,
                DateOut = source.DateOut,
                DateIn = source.DateIn,
                Quantity = source.Quantity,
                AvailableQuantity = source.AvailableQuantity,
                Needs = quantity,
                DeliveryDateTime = source.DeliveryDateTime,
                OrderDeliveryDateTime = orderDeliveryDateTime,
                OrderState = orderState
            };
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

        private PurchaseSourceSaveDto GetPurchaseSaveSource()
        {
            return PurchaseSourceSaveDto.Movement(
                SelectedMovement.MovementId,
                SelectedMovement.WarehouseFromName,
                SelectedMovement.WarehouseToId,
                SelectedMovement.WarehouseToName,
                SelectedMovement.DateOut,
                SelectedMovement.DateIn);
        }
    }
}