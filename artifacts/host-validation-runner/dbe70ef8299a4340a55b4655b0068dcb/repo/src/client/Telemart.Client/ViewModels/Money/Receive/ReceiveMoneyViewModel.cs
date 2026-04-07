using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using Telemart.Client.Business;
using Telemart.Client.Common;
using Telemart.Client.Common.Services;
using Telemart.Client.Core.Exceptions;
using Telemart.Client.Data.Requests.Features.Order.Actions;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.Properties;
using Telemart.Client.TransferObjects;
using Telemart.Client.ViewModels.Base;
using Telemart.Client.ViewModels.Validation;

namespace Telemart.Client.ViewModels.Money.Receive
{
    public sealed class ReceiveMoneyViewModel : TelemartDialogViewModelBase
    {
        private int number;

        public ReceiveMoneyViewModel(IWebClient webClient, IDictionaries dictionaries, IMessageFacadeService messageFacadeService)
            : base(webClient, dictionaries, messageFacadeService)
        {
            RemoveAllCommand = new DelegateCommand(RemoveAll);
            RemoveItemCommand = new DelegateCommand<ReceiveViewItem>(RemoveItem, x => x != null);
            RecognizeAllCommand = new DelegateCommand(RecognizeAll);
            RecognizeCommand = new DelegateCommand(Recognize);
            HandleActualAmountChangedCommand = new DelegateCommand(RefreshSummaryItems);
        }

        public ReceiveMoneyViewModel()
        {
        }

        #region Commands

        public IDelegateCommand RemoveAllCommand { get; }

        public IDelegateCommand RemoveItemCommand { get; }

        public IDelegateCommand RecognizeAllCommand { get; }

        public IDelegateCommand RecognizeCommand { get; }

        public IDelegateCommand HandleActualAmountChangedCommand { get; }

        #endregion

        #region INPC

        public ObservableCollection<ReceiveViewMessageItem> Messages
        {
            get { return GetProperty(() => Messages); }
            private set { SetProperty(() => Messages, value); }
        }

        public ReceiveViewMessageItem CurrentMessage
        {
            get { return GetProperty(() => CurrentMessage); }
            set { SetProperty(() => CurrentMessage, value); }
        }

        public IEnumerable<SummaryViewItem> SummaryItems
        {
            get { return GetProperty(() => SummaryItems); }
            private set { SetProperty(() => SummaryItems, value); }
        }

        public ObservableCollection<ReceiveViewItem> Items
        {
            get { return GetProperty(() => Items); }
            private set { SetProperty(() => Items, value); }
        }

        public ReceiveViewItem CurrentItem
        {
            get { return GetProperty(() => CurrentItem); }
            set { SetProperty(() => CurrentItem, value); }
        }

        public string Barcode
        {
            get { return GetProperty(() => Barcode); }
            set { SetProperty(() => Barcode, value); }
        }

        public MoneyReceiveRecognizeType RecognizeType
        {
            get { return GetProperty(() => RecognizeType); }
            set { SetProperty(() => RecognizeType, value); }
        }

        #endregion

        #region DialogSettings

        public override int Height => 600;

        public override int MinHeight => 450;

        public override int MinWidth => 750;

        public override int Width => 800;

        #endregion

        protected override Task HandleLoadedAsync()
        {
            Messages = new ObservableCollection<ReceiveViewMessageItem>();
            Items = new ObservableCollection<ReceiveViewItem>();
            Items.CollectionChanged += ItemsCollectionChanged;

            RecognizeType = (MoneyReceiveRecognizeType)Parameter;

            RefreshSummaryItems();

            Title = RecognizeType.Title;

            return Task.CompletedTask;
        }

        protected override Task HandleOkAsync()
        {
            if (!Items.Any())
            {
                MessageFacadeService.ShowNotificationWarning("Нечего добавлять");
                Close();
                return Task.CompletedTask;
            }

            if (Items.Any(x => x.ActualAmount <= 0))
            {
                MessageFacadeService.ShowNotificationError("Запрещено проводить заказы, где Факт = 0");
                return Task.CompletedTask;
            }

            if (Items.Any(x => !x.Recognized))
            {
                MessageFacadeService.ShowNotificationError("Не все счета распознаны");
                return Task.CompletedTask;
            }

            ValidationResultItem[] validationItems = CanProcess().ToArray();

            if (validationItems.Any() && !ShowValidationResultView("Ошибки", validationItems))
            {
                return Task.CompletedTask;
            }

            IsOk = true;
            Close();

            return Task.CompletedTask;

            IEnumerable<ValidationResultItem> CanProcess()
            {
                string[] notRecognizedNumbers = Items.Where(i => !i.Recognized || i.Recognizing).Select(i => i.Number.ToString()).ToArray();

                if (notRecognizedNumbers.Any())
                {
                    yield return new ValidationResultItem($"Не {GetRecognizedLinesTextByLinesCount(notRecognizedNumbers)} №{string.Join(", ", notRecognizedNumbers)}", true);
                }

                string[] actualGreaterThanPlannedNumbers = Items.Where(i => i.ActualAmount > i.PlannedAmount).Select(i => i.Number.ToString()).ToArray();

                if (actualGreaterThanPlannedNumbers.Any())
                {
                    yield return new ValidationResultItem($"В {GetLinesTextByLinesCount(actualGreaterThanPlannedNumbers)} №{string.Join(", ", actualGreaterThanPlannedNumbers)} \"факт\" больше \"плана\"", true);
                }

                string[] actualLessThanPlannedNumbers = Items.Where(i => i.ActualAmount < i.PlannedAmount).Select(i => i.Number.ToString()).ToArray();

                if (actualLessThanPlannedNumbers.Any())
                {
                    yield return new ValidationResultItem($"В {GetLinesTextByLinesCount(actualLessThanPlannedNumbers)} №{string.Join(", ", actualLessThanPlannedNumbers)} \"факт\" не равен \"плану\"", false);
                }
            }

            string GetLinesTextByLinesCount(IReadOnlyCollection<string> lines)
            {
                return lines.Count == 1
                    ? "строке"
                    : "строках";
            }

            string GetRecognizedLinesTextByLinesCount(IReadOnlyCollection<string> lines)
            {
                return lines.Count == 1
                    ? "распознана строка"
                    : "распознаны строки";
            }
        }

        protected override void OnInitializeInDesignMode()
        {
            base.OnInitializeInDesignMode();

            Items = new ObservableCollection<ReceiveViewItem>();
            Messages = new ObservableCollection<ReceiveViewMessageItem>();

            Random random = new Random();

            for (int i = 0; i < 6; i++)
            {
                Items.Add(new ReceiveViewItem(i + 1, string.Empty)
                {
                    OrderId = random.Next(100000, 300000),
                    PlannedAmount = random.Next(100000),
                    ActualAmount = random.Next(100000),
                    Recognized = random.NextDouble() > 0.5,
                    Processed = random.NextDouble() < 0.5,
                    OrderState = OrderStatus.Done
                });
            }

            Messages = new ObservableCollection<ReceiveViewMessageItem>
            {
                new ReceiveViewMessageItem(1, "Error 1"),
                new ReceiveViewMessageItem(2, "Error 2"),
                new ReceiveViewMessageItem(3, "Error 3"),
                new ReceiveViewMessageItem(4, "Error 4"),
                new ReceiveViewMessageItem(5, "Error 5")
            };

            RefreshSummaryItems();
        }

        private void ItemsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            RefreshSummaryItems();
        }

        private void Recognize()
        {
            string text = Barcode;

            Barcode = string.Empty;

            string[] errors = RecognizeType.RecognizeStrategy.Validate(text, Items).ToArray();

            if (errors.Any())
            {
                MessageFacadeService.ShowNotificationWarning(string.Join(Environment.NewLine, errors));
            }
            else
            {
                ReceiveViewItem item = RecognizeType.RecognizeStrategy.CreateViewItem(++number, text);

                Items.Add(item);
                CurrentItem = item;

                RecognizeItem(item);
            }
        }

        private void RecognizeAll()
        {
            ReceiveViewItem[] notRecognizedItems = Items.Where(x => !x.Recognized).ToArray();

            if (notRecognizedItems.Any())
            {
                foreach (ReceiveViewItem item in notRecognizedItems)
                {
                    RecognizeItem(item);
                }
            }
            else
            {
                MessageFacadeService.ShowNotificationWarning("Все записи уже распознаны");
            }
        }

        private void RecognizeItem(ReceiveViewItem item)
        {
            item.Recognizing = true;

            SearchOrderRequest request = RecognizeType.RecognizeStrategy.GetOrderRequest(item.Raw);

            WebClient.ExecuteApiRequestAsync(request)
                .ContinueWith(
                    t =>
                    {
                        if (t.Status == TaskStatus.RanToCompletion)
                        {
                            OrderReceiveMoneyDto data = t.Result.Data;

                            item.OrderId = data.Id;
                            item.OrderState = Dictionaries.GetItemById<OrderStatus>(data.StateId);
                            item.PlannedAmount = data.LeftToPayUah;
                            item.ActualAmount = item.PlannedAmount;
                            item.Recognized = true;
                        }

                        if (t.IsFaulted)
                        {
                            Exception exception = t.Exception?.Flatten().InnerException;

                            string message;

                            if (exception is UnexpectedSatusException unexpectedSatusException)
                            {
                                message = string.Join(", ", unexpectedSatusException.GetErrorItems().Select(x => x.Message));
                            }
                            else if (exception is UnexpectedErrorException)
                            {
                                message = $"{Resources.ServerConnectError}. {Resources.ServerUnavailable}";
                            }
                            else
                            {
                                message = Resources.ErrorExecutingOperation;
                            }

                            AddMessage(item, message);
                        }

                        item.Recognizing = false;

                        RefreshSummaryItems();
                    },
                    TaskScheduler.FromCurrentSynchronizationContext());
        }

        private void AddMessage(ReceiveViewItem item, string message)
        {
            ReceiveViewMessageItem messageItem = new ReceiveViewMessageItem(item.Number, message);
            Messages.Add(messageItem);
            CurrentMessage = messageItem;
        }

        private void RefreshSummaryItems()
        {
            SummaryItems = GetSummaryItems();

            IEnumerable<SummaryViewItem> GetSummaryItems()
            {
                int recognized = 0;
                decimal plannedAmount = 0;
                decimal actualAmount = 0;

                foreach (ReceiveViewItem item in Items.ToArray())
                {
                    plannedAmount += item.PlannedAmount;
                    actualAmount += item.ActualAmount;

                    if (item.Recognized)
                    {
                        recognized++;
                    }
                }

                yield return new SummaryViewItem("Всего", $"{Items.Count:D} шт.");
                yield return new SummaryViewItem("Распознано", $"{recognized:D} шт.");
                yield return new SummaryViewItem("План", CurrencyFormatingRules.ToUahStr(plannedAmount, "C2"));
                yield return new SummaryViewItem("Факт", CurrencyFormatingRules.ToUahStr(actualAmount, "C2"));
            }
        }

        private void RemoveAll()
        {
            if (MessageFacadeService.Confirm("Вы уверены?"))
            {
                Items.Clear();
                Messages.Clear();
                number = 0;
            }
        }

        private void RemoveItem(ReceiveViewItem item)
        {
            if (MessageFacadeService.Confirm("Вы уверены?"))
            {
                Items.Remove(item);
            }
        }
    }
}