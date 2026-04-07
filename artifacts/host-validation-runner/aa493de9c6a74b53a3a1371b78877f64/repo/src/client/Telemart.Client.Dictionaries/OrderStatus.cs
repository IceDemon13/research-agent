using System;

#pragma warning disable SA1402 // File may only contain a single class

namespace Telemart.Client.Dictionaries
{
    public abstract class OrderStatus : DictionaryItem, IComparable<OrderStatus>
    {
        protected const int ReceivedId = 0;
        protected const int ConfirmedId = 1;
        protected const int DidNotOrderId = 2;
        protected const int PackedId = 3;
        protected const int DoneId = 4;
        protected const int CancelledId = 5;
        protected const int ReturnedId = 6;
        protected const int DidNotTakeId = 7;
        protected const int NewId = 8;

        protected OrderStatus(int id, string name)
            : base(id, name, true)
        {
        }

        public static OrderStatus Received { get; } = new ReceivedOrderStatus();

        public static OrderStatus Confirmed { get; } = new ConfirmedOrderStatus();

        public static OrderStatus DidNotOrder { get; } = new DidNotOrderOrderStatus();

        public static OrderStatus Packed { get; } = new PackedOrderStatus();

        public static OrderStatus Done { get; } = new DoneOrderStatus();

        public static OrderStatus Canceled { get; } = new CanceledOrderStatus();

        public static OrderStatus Returned { get; } = new ReturnedOrderStatus();

        public static OrderStatus DidNotTake { get; } = new DidNotTakenOrderStatus();

        public static OrderStatus New { get; } = new NewOrderStatus();

        public abstract bool CanAddProduct { get; }

        public virtual bool CalcProductDateX => false;

        public virtual bool CanChangeSources => false;

        public virtual bool CanChangeStates => false;

        public abstract int Range { get; }

        public static bool operator <(OrderStatus s1, OrderStatus s2)
        {
            return s1.Range < s2.Range;
        }

        public static bool operator >(OrderStatus s1, OrderStatus s2)
        {
            return s1.Range > s2.Range;
        }

        public static bool operator <=(OrderStatus s1, OrderStatus s2)
        {
            return s1.Range <= s2.Range;
        }

        public static bool operator >=(OrderStatus s1, OrderStatus s2)
        {
            return s1.Range >= s2.Range;
        }

        public int CompareTo(OrderStatus other)
        {
            return Range.CompareTo(other.Range);
        }
    }

    public sealed class ReceivedOrderStatus : OrderStatus
    {
        public ReceivedOrderStatus()
            : base(ReceivedId, "Принят")
        {
        }

        public override bool CanAddProduct { get; } = true;

        public override bool CalcProductDateX { get; } = true;

        public override bool CanChangeSources { get; } = true;

        public override bool CanChangeStates { get; } = true;

        public override int Range => 0;
    }

    public sealed class ConfirmedOrderStatus : OrderStatus
    {
        public ConfirmedOrderStatus()
            : base(ConfirmedId, "Подтвержден")
        {
        }

        public override bool CanAddProduct { get; } = true;

        public override bool CalcProductDateX { get; } = true;

        public override bool CanChangeSources { get; } = true;

        public override int Range => 1;
    }

    public sealed class DidNotOrderOrderStatus : OrderStatus
    {
        public DidNotOrderOrderStatus()
            : base(DidNotOrderId, "Не заказали")
        {
        }

        public override bool CanAddProduct { get; } = false;

        public override int Range => 5;
    }

    public sealed class PackedOrderStatus : OrderStatus
    {
        public PackedOrderStatus()
            : base(PackedId, "Упакован")
        {
        }

        public override bool CanAddProduct { get; } = true;

        public override int Range => 3;
    }

    public sealed class DoneOrderStatus : OrderStatus
    {
        public DoneOrderStatus()
            : base(DoneId, "Выполнен")
        {
        }

        public override bool CanAddProduct { get; } = false;

        public override int Range => 4;
    }

    public sealed class CanceledOrderStatus : OrderStatus
    {
        public CanceledOrderStatus()
            : base(CancelledId, "Отменен")
        {
        }

        public override bool CanAddProduct { get; } = false;

        public override int Range => 5;
    }

    public sealed class ReturnedOrderStatus : OrderStatus
    {
        public ReturnedOrderStatus()
            : base(ReturnedId, "Возвращен")
        {
        }

        public override bool CanAddProduct { get; } = false;

        public override int Range => 5;
    }

    public sealed class DidNotTakenOrderStatus : OrderStatus
    {
        public DidNotTakenOrderStatus()
            : base(DidNotTakeId, "Не забран")
        {
        }

        public override bool CanAddProduct { get; } = false;

        public override int Range => 5;
    }

    public sealed class NewOrderStatus : OrderStatus
    {
        public NewOrderStatus()
            : base(NewId, "Оформляется")
        {
        }

        public override bool CanAddProduct { get; } = false;

        public override int Range => 5;
    }
}