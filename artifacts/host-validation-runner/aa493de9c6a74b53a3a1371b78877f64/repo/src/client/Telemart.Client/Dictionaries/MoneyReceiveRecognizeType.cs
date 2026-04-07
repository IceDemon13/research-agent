using System;
using Telemart.Client.ViewModels.Money.Receive.RecognizeStrategy;

namespace Telemart.Client.Dictionaries
{
    public sealed class MoneyReceiveRecognizeType : DictionaryItem
    {
        public const int TtnId = 1;
        public const int OrderId = 2;
        public const int ExcelId = 3;
        public const int ExcelUkrposhtaId = 4;
        public const int ExcelMeestTtnId = 5;

        private readonly Lazy<IRecognizeStrategy> recognizeStrategyLazy;

        private MoneyReceiveRecognizeType(int id, string name, string title, string barcodeLabel, Func<IRecognizeStrategy> recognizeStrategyResolver)
            : base(id, name, true)
        {
            Title = title;
            BarcodeLabel = barcodeLabel;
            recognizeStrategyLazy = new Lazy<IRecognizeStrategy>(recognizeStrategyResolver);
        }

        public static MoneyReceiveRecognizeType Ttn { get; } = new MoneyReceiveRecognizeType(TtnId, "ТТН", "Реестр НП", "ТТН", () => new TrackNumberRecognizeStrategy());

        public static MoneyReceiveRecognizeType Order { get; } = new MoneyReceiveRecognizeType(OrderId, "Заказ", "Реестр доставок", "Номер заказа", () => new OrderRecognizeStrategy());

        public static MoneyReceiveRecognizeType Excel { get; } = new MoneyReceiveRecognizeType(ExcelId, "Excel", "Из Excel (по № заказа)", string.Empty, () => null);

        public static MoneyReceiveRecognizeType ExcelUkrposhta { get; } = new MoneyReceiveRecognizeType(ExcelUkrposhtaId, "Excel Укрпочта", "Из Excel (по ШК Укрпочты)", string.Empty, () => null);

        public static MoneyReceiveRecognizeType ExcelMeestTtn { get; } = new MoneyReceiveRecognizeType(ExcelMeestTtnId, "Excel ТТН", "Из Excel (по ТТН)", string.Empty, () => null);

        public string Title { get; }

        public string BarcodeLabel { get; }

        public IRecognizeStrategy RecognizeStrategy => recognizeStrategyLazy.Value;
    }
}