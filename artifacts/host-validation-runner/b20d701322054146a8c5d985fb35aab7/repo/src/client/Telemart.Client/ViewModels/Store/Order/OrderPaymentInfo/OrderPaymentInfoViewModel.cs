using System;
using System.Collections.ObjectModel;
using System.Linq;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using Telemart.Client.Business;

namespace Telemart.Client.ViewModels.Store.Order.OrderPaymentInfo
{
    public sealed class OrderPaymentInfoViewModel : BindableBase
    {
        public OrderPaymentInfoViewModel()
        {
            SummaryWithoutDiscountItem = new OrderPaymentInfoItem(0, "Без скидки:", 0, 0, 0);
            DiscountItem = new OrderPaymentInfoItem(1, "Скидка:", 0, 0, 0);
            BonusesItem = new OrderPaymentInfoItem(2, "В т.ч. бонусы:", 0, 0, 0);
            SummaryItem = new OrderPaymentInfoItem(3, "Итого:", 0, 0, 0);
            DeliveryItem = new OrderPaymentInfoItem(4, "Доставка:", 0, 0, 0);
            PrepaymentItem = new OrderPaymentInfoItem(5, "Предоплата:", 0, 0, 0);
            ToPayItem = new OrderPaymentInfoItem(6, "К оплате:", 0, 0, 0);
            BonusesToChargeItem = new OrderPaymentInfoItem(7, "Бонусов за заказ:", 0, 0, 0);
            MoneyBackAmountItem = new OrderPaymentInfoItem(8, "Налож. платеж:", 0, 0, 0);


            Items = new ObservableCollection<OrderPaymentInfoItem>
            {
                SummaryItem,
                DeliveryItem,
                PrepaymentItem,
                BonusesItem,
                ToPayItem,
                BonusesToChargeItem,
                MoneyBackAmountItem
            };
        }

        public OrderPaymentInfoItem SummaryWithoutDiscountItem { get; }

        public OrderPaymentInfoItem DiscountItem { get; }

        public OrderPaymentInfoItem DeliveryItem { get; }

        public OrderPaymentInfoItem PrepaymentItem { get; }

        public OrderPaymentInfoItem SummaryItem { get; }

        public OrderPaymentInfoItem ToPayItem { get; }

        public OrderPaymentInfoItem BonusesToChargeItem { get; }

        public OrderPaymentInfoItem BonusesItem { get; }

        public OrderPaymentInfoItem MoneyBackAmountItem { get; }

        public ObservableCollection<OrderPaymentInfoItem> Items { get; private set; }

        public static Prices CalcToPayAfterUseBonuses(int bonusesQuantity, IOrderPaymentInfo order)
        {
            if (order == null)
            {
                throw new ArgumentNullException(nameof(order));
            }

            Prices summary = new Prices(0, 0, 0);
            Prices prepayment = new Prices(0, 0, 0);
            Prices delivery = new Prices(order.PackageDeliveryCost ?? 0, 0, 0);

            foreach (IOrderPaymentInfoProduct x in order.GetOrderProducts())
            {
                Price cost = x.Price * x.Quantity;

                summary += cost;
            }

            foreach (Price payment in order.GetOrderPayments().Select(x => new Price(x.Sign * x.Amount, x.CurrencyId)))
            {
                prepayment += payment;
            }

            if (summary.Uah >= order.MinPriceFreeDelivery)
            {
                delivery = new Prices(0, 0, 0);
            }

            return summary + delivery - prepayment - new Prices(bonusesQuantity, 0, 0);
        }

        public void CalcPaymentInfo(IOrderPaymentInfo order)
        {
            if (order == null)
            {
                throw new ArgumentNullException(nameof(order));
            }

            Prices summary = new Prices(0, 0, 0);
            Prices summaryWithoutDiscount = new Prices(0, 0, 0);
            Prices prepayment = new Prices(0, 0, 0);
            Prices discount = new Prices(0, 0, 0);
            Prices delivery = new Prices(order.PackageDeliveryCost ?? 0, 0, 0);
            Prices moneyBack = new Prices(order.MoneyBackAmount ?? -1, 0, 0);

            foreach (IOrderPaymentInfoProduct x in order.GetOrderProducts())
            {
                Price cost = x.Price * x.Quantity;

                summary += cost;

                if (x.OriginalPrice.CurrencyId == x.Price.CurrencyId && x.Price.Value < x.OriginalPrice.Value)
                {
                    discount += new Price((x.OriginalPrice.Value - x.Price.Value) * x.Quantity, x.OriginalPrice.CurrencyId);

                    summaryWithoutDiscount += x.OriginalPrice * x.Quantity;
                }
                else
                {
                    summaryWithoutDiscount += cost;
                }
            }

            foreach (Price payment in order.GetOrderPayments().Select(x => new Price(x.Sign * x.Amount, x.CurrencyId)))
            {
                prepayment += payment;
            }

            if (summary.Uah >= order.MinPriceFreeDelivery)
            {
                delivery = new Prices(0, 0, 0);
            }

            SummaryWithoutDiscountItem.Value = summaryWithoutDiscount;
            DiscountItem.Value = discount;
            PrepaymentItem.Value = prepayment;
            DeliveryItem.Value = delivery;
            SummaryItem.Value = summary;
            BonusesItem.Value = new Prices(order.GetBonusesQuantity(), 0, 0);
            ToPayItem.Value = summary + delivery - prepayment;
            BonusesToChargeItem.Value = new Prices(order.GetBonusesToChargeQuantity(), 0, 0);
            MoneyBackAmountItem.Value = moneyBack;

            if (discount.Uah > 0 || discount.Usd > 0)
            {
                if (!Items.Contains(DiscountItem))
                {
                    Items.Add(DiscountItem);
                }

                if (!Items.Contains(SummaryWithoutDiscountItem))
                {
                    Items.Add(SummaryWithoutDiscountItem);
                }
            }
            else
            {
                Items.Remove(SummaryWithoutDiscountItem);
                Items.Remove(DiscountItem);
            }

            if (BonusesToChargeItem.Value == default)
            {
                Items.Remove(BonusesToChargeItem);
            }
            else if (!Items.Contains(BonusesToChargeItem))
            {
                Items.Add(BonusesToChargeItem);
            }

            if (BonusesItem.Value == default)
            {
                Items.Remove(BonusesItem);
            }
            else if (!Items.Contains(BonusesItem))
            {
                Items.Add(BonusesItem);
            }

            if (MoneyBackAmountItem.Value.Uah < 0)
            {
                Items.Remove(MoneyBackAmountItem);
            }
            else if (!Items.Contains(MoneyBackAmountItem))
            {
                Items.Add(MoneyBackAmountItem);
            }


            Items = Items.OrderBy(x => x.Order).ToObservableCollection();
        }
    }
}